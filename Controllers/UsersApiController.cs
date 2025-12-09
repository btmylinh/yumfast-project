using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using BCrypt.Net;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/users")]
    public class UsersApiController : ControllerBase
    {
        private readonly IConfiguration _config;
        public UsersApiController(IConfiguration config) { _config = config; }

        [HttpGet]
        public IActionResult Get([FromQuery] string? search, [FromQuery] int? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? sortBy = "created_at", [FromQuery] string? sortDirection = "desc")
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            var where = "WHERE 1=1";
            if (!string.IsNullOrWhiteSpace(search)) where += " AND (name ILIKE @s OR email ILIKE @s OR phone ILIKE @s)";
            if (status.HasValue) where += " AND status=@st";
            var sort = (sortBy?.ToLower()) switch
            {
                "name" => "name",
                "email" => "email",
                "status" => "status",
                _ => "created_at"
            };
            var dir = sortDirection?.ToLower() == "asc" ? "ASC" : "DESC";

            using var countCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM users {where}", conn);
            if (!string.IsNullOrWhiteSpace(search)) countCmd.Parameters.AddWithValue("@s", $"%{search}%");
            if (status.HasValue) countCmd.Parameters.AddWithValue("@st", status.Value);
            var totalCount = (long)countCmd.ExecuteScalar();

            var offset = (page - 1) * pageSize;
            using var cmd = new NpgsqlCommand($"SELECT id, name, email, phone, role, status, created_at FROM users {where} ORDER BY {sort} {dir} LIMIT @ps OFFSET @off", conn);
            if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@s", $"%{search}%");
            if (status.HasValue) cmd.Parameters.AddWithValue("@st", status.Value);
            cmd.Parameters.AddWithValue("@ps", pageSize);
            cmd.Parameters.AddWithValue("@off", offset);
            var list = new List<UserDto>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new UserDto
                {
                    Id = r.GetInt64(0),
                    Name = r.GetString(1),
                    Email = r.GetString(2),
                    Phone = r.IsDBNull(3) ? string.Empty : r.GetString(3),
                    Role = r.GetString(4),
                    Status = r.GetInt16(5),
                    CreatedAt = r.GetDateTime(6)
                });
            }
            return Ok(new { data = list, total = totalCount });
        }

        [HttpGet("{id}")]
        public IActionResult GetById(long id)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT id, name, email, phone, role, status, created_at, updated_at FROM users WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var r = cmd.ExecuteReader();
            if (!r.Read()) return NotFound(new { message = "not_found" });
            return Ok(new UserDetailDto
            {
                Id = r.GetInt64(0),
                Name = r.GetString(1),
                Email = r.GetString(2),
                Phone = r.IsDBNull(3) ? string.Empty : r.GetString(3),
                Role = r.GetString(4),
                Status = r.GetInt16(5),
                CreatedAt = r.GetDateTime(6),
                UpdatedAt = r.GetDateTime(7)
            });
        }

        [HttpPost]
        public IActionResult Create([FromBody] UserManageRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { message = "validation_failed" });
            var role = string.IsNullOrWhiteSpace(req.Role) ? "user" : req.Role.Trim().ToLower();
            if (role != "user" && role != "admin") return BadRequest(new { message = "invalid_role" });

            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            using (var dup = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE email=@e", conn))
            {
                dup.Parameters.AddWithValue("@e", req.Email);
                var exists = (long)dup.ExecuteScalar();
                if (exists > 0) return Conflict(new { message = "email_exists" });
            }

            var hashed = BCrypt.Net.BCrypt.HashPassword(req.Password);
            using var cmd = new NpgsqlCommand(@"INSERT INTO users(name,email,phone,password,avatar,role,status) VALUES(@n,@e,@p,@pw,'default.png',@r,@st) RETURNING id", conn);
            cmd.Parameters.AddWithValue("@n", req.Name);
            cmd.Parameters.AddWithValue("@e", req.Email);
            cmd.Parameters.AddWithValue("@p", (object?)req.Phone ?? string.Empty);
            cmd.Parameters.AddWithValue("@pw", hashed);
            cmd.Parameters.AddWithValue("@r", role);
            cmd.Parameters.AddWithValue("@st", req.Status.HasValue ? req.Status.Value : 1);
            var id = (long)cmd.ExecuteScalar();
            return Ok(new { id });
        }

        [HttpPut("{id}")]
        public IActionResult Update(long id, [FromBody] UserManageRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Email))
                return BadRequest(new { message = "validation_failed" });
            var role = string.IsNullOrWhiteSpace(req.Role) ? "user" : req.Role.Trim().ToLower();
            if (role != "user" && role != "admin") return BadRequest(new { message = "invalid_role" });

            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            using (var dup = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE email=@e AND id<>@id", conn))
            {
                dup.Parameters.AddWithValue("@e", req.Email);
                dup.Parameters.AddWithValue("@id", id);
                var exists = (long)dup.ExecuteScalar();
                if (exists > 0) return Conflict(new { message = "email_exists" });
            }

            var sql = "UPDATE users SET name=@n, email=@e, phone=@p, role=@r, status=@st";
            var hasPwd = !string.IsNullOrWhiteSpace(req.Password);
            if (hasPwd) sql += ", password=@pw";
            sql += ", updated_at=NOW() WHERE id=@id";
            using var cmd = new NpgsqlCommand(sql, conn);
            cmd.Parameters.AddWithValue("@n", req.Name);
            cmd.Parameters.AddWithValue("@e", req.Email);
            cmd.Parameters.AddWithValue("@p", (object?)req.Phone ?? string.Empty);
            cmd.Parameters.AddWithValue("@r", role);
            cmd.Parameters.AddWithValue("@st", req.Status.HasValue ? req.Status.Value : 1);
            if (hasPwd)
            {
                var hashed = BCrypt.Net.BCrypt.HashPassword(req.Password!);
                cmd.Parameters.AddWithValue("@pw", hashed);
            }
            cmd.Parameters.AddWithValue("@id", id);
            var rows = cmd.ExecuteNonQuery();
            if (rows == 0) return NotFound(new { message = "not_found" });
            return Ok(new { id });
        }

        [HttpPatch("{id}/status")]
        public IActionResult ToggleStatus(long id)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            using var getCmd = new NpgsqlCommand("SELECT status FROM users WHERE id=@id", conn);
            getCmd.Parameters.AddWithValue("@id", id);
            var cur = getCmd.ExecuteScalar();
            if (cur == null) return NotFound(new { message = "not_found" });
            var next = ((short)cur) == 1 ? 0 : 1;
            using var upd = new NpgsqlCommand("UPDATE users SET status=@st WHERE id=@id", conn);
            upd.Parameters.AddWithValue("@st", next);
            upd.Parameters.AddWithValue("@id", id);
            upd.ExecuteNonQuery();
            return Ok(new { status = next });
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(long id)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM users WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            var rows = cmd.ExecuteNonQuery();
            if (rows == 0) return NotFound(new { message = "not_found" });
            return Ok(new { id });
        }
    }

    public class UserDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Role { get; set; } = string.Empty;
        public short Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    public class UserDetailDto : UserDto
    {
        public DateTime UpdatedAt { get; set; }
    }

    public class UserManageRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Phone { get; set; }
        public string? Password { get; set; }
        public string Role { get; set; } = "user";
        public int? Status { get; set; } = 1;
    }
}
