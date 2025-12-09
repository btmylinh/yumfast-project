using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/categories")]
    public class CategoriesApiController : ControllerBase
    {
        private readonly IConfiguration _config;

        public CategoriesApiController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet]
        public IActionResult Get([FromQuery] string? search, [FromQuery] int? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 10, [FromQuery] string? sortBy = "created_at", [FromQuery] string? sortDirection = "desc")
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var where = "WHERE 1=1";
            if (!string.IsNullOrWhiteSpace(search)) where += " AND (name ILIKE @s OR slug ILIKE @s)";
            if (status.HasValue) where += " AND status=@st";

            var sort = (sortBy?.ToLower()) switch
            {
                "name" => "name",
                "status" => "status",
                _ => "created_at"
            };
            var dir = sortDirection?.ToLower() == "asc" ? "ASC" : "DESC";

            using var countCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM categories {where}", conn);
            if (!string.IsNullOrWhiteSpace(search)) countCmd.Parameters.AddWithValue("@s", $"%{search}%");
            if (status.HasValue) countCmd.Parameters.AddWithValue("@st", status.Value);
            var totalCount = (long)countCmd.ExecuteScalar();

            var offset = (page - 1) * pageSize;
            using var cmd = new NpgsqlCommand($"SELECT id, name, slug, description, status, created_at, updated_at FROM categories {where} ORDER BY {sort} {dir} LIMIT @ps OFFSET @off", conn);
            if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@s", $"%{search}%");
            if (status.HasValue) cmd.Parameters.AddWithValue("@st", status.Value);
            cmd.Parameters.AddWithValue("@ps", pageSize);
            cmd.Parameters.AddWithValue("@off", offset);

            var list = new List<CategoryDto>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                list.Add(new CategoryDto
                {
                    Id = reader.GetInt64(0),
                    Name = reader.GetString(1),
                    Slug = reader.GetString(2),
                    Description = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                    Status = reader.GetInt16(4),
                    CreatedAt = reader.GetDateTime(5),
                    UpdatedAt = reader.GetDateTime(6)
                });
            }

            var totalPages = (int)Math.Ceiling(totalCount / (double)pageSize);
            return Ok(new { data = list, totalCount, totalPages });
        }

        [HttpPost]
        public IActionResult Create([FromBody] CategoryCreateRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name) || string.IsNullOrWhiteSpace(req.Slug))
                return BadRequest(new { message = "validation_fill_all" });

            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            using var cmd = new NpgsqlCommand("INSERT INTO categories(name, slug, description, status) VALUES(@n,@sl,@d,@st) RETURNING id", conn);
            cmd.Parameters.AddWithValue("@n", req.Name);
            cmd.Parameters.AddWithValue("@sl", req.Slug);
            cmd.Parameters.AddWithValue("@d", (object?)req.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("@st", req.Status);
            var id = (long)cmd.ExecuteScalar();
            return Ok(new { id });
        }

        [HttpPut("{id}")]
        public IActionResult Update(long id, [FromBody] CategoryUpdateRequest req)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            using var cmd = new NpgsqlCommand("UPDATE categories SET name=@n, slug=@sl, description=@d, status=@st WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@n", req.Name);
            cmd.Parameters.AddWithValue("@sl", req.Slug);
            cmd.Parameters.AddWithValue("@d", (object?)req.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("@st", req.Status);
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
            using var getCmd = new NpgsqlCommand("SELECT status FROM categories WHERE id=@id", conn);
            getCmd.Parameters.AddWithValue("@id", id);
            var cur = getCmd.ExecuteScalar();
            if (cur == null) return NotFound(new { message = "not_found" });
            var next = ((short)cur) == 1 ? 0 : 1;
            using var upd = new NpgsqlCommand("UPDATE categories SET status=@st WHERE id=@id", conn);
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
            using var cmd = new NpgsqlCommand("DELETE FROM categories WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            var rows = cmd.ExecuteNonQuery();
            if (rows == 0) return NotFound(new { message = "not_found" });
            return Ok(new { id });
        }
    }

    public class CategoryDto
    {
        public long Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public short Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime UpdatedAt { get; set; }
    }

    public class CategoryCreateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public short Status { get; set; } = 1;
    }

    public class CategoryUpdateRequest
    {
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string? Description { get; set; }
        public short Status { get; set; } = 1;
    }
}
