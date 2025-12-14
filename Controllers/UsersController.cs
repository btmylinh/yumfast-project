using System.Security.Claims;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using WebApp.Services;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/[controller]")] // Route API Admin
    public class UsersController : BaseController
    {
        private readonly IConfiguration _config;
        private readonly IJsonLocalizationService _localization;

        public UsersController(IJsonLocalizationService localizationService, IConfiguration config) : base(localizationService)
        {
            _localization = localizationService;
            _config = config;
        }

        // ============================================================
        // ================  🔵  REGION 1: MVC VIEWS  ==================
        // ============================================================

        #region MVC VIEWS

        /// <summary>
        /// Trang hồ sơ người dùng (MVC)
        /// </summary>
        [Route("~/user/profile")]
        public IActionResult Profile()
        {
            return View("~/Views/User/Profile/Settings.cshtml");
        }

        /// <summary>
        /// Trang đơn hàng của người dùng - Redirect đến MyOrders
        /// </summary>
        [Route("~/user/orders")]
        public IActionResult Orders()
        {
            return RedirectToAction("MyOrders", "Order");
        }

        /// <summary>
        /// Trang cài đặt tài khoản
        /// </summary>
        [Route("~/user/settings")]
        public IActionResult Settings()
        {
            return View();
        }

        /// <summary>
        /// Xử lý cập nhật thông tin người dùng (MVC POST)
        /// </summary>
        [HttpPost("~/user/updateprofile")]
        [ValidateAntiForgeryToken]
        public IActionResult UpdateProfile(string fullName, string email, string phone, string address)
        {
            if (ModelState.IsValid)
            {
                TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
                return RedirectToAction("Profile");
            }

            return View("Profile");
        }

        /// <summary>
        /// Trang địa chỉ người dùng
        /// </summary>
        [Route("~/user/address")]
        public IActionResult Address()
        {
            return View();
        }

        /// <summary>
        /// Trang phương thức thanh toán
        /// </summary>
        [Route("~/user/paymentmethod")]
        public IActionResult PaymentMethod()
        {
            return View();
        }

        #endregion

        // ============================================================
        // ================  🔵  REGION 2: API ADMIN  ==================
        // ============================================================

        #region API ADMIN ( /api/users/... )

        /// <summary>
        /// API lấy danh sách người dùng (lọc, phân trang, sắp xếp)
        /// </summary>
        [HttpGet("/api/users")]
        public IActionResult ApiUsersGet(
            [FromQuery] string? search,
            [FromQuery] int? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10,
            [FromQuery] string? sortBy = "created_at",
            [FromQuery] string? sortDirection = "desc")
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

            using var cmd = new NpgsqlCommand(
                $"SELECT id, name, email, phone, role, status, created_at FROM users {where} ORDER BY {sort} {dir} LIMIT @ps OFFSET @off",
                conn
            );

            if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@s", $"%{search}%");
            if (status.HasValue) cmd.Parameters.AddWithValue("@st", status.Value);
            cmd.Parameters.AddWithValue("@ps", pageSize);
            cmd.Parameters.AddWithValue("@off", offset);

            var list = new List<object>();
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new
                {
                    Id = r.GetInt64(0),
                    Name = r.GetString(1),
                    Email = r.GetString(2),
                    Phone = r.IsDBNull(3) ? "" : r.GetString(3),
                    Role = r.GetString(4),
                    Status = r.GetInt16(5),
                    CreatedAt = r.GetDateTime(6)
                });
            }

            return Ok(new { data = list, total = totalCount });
        }

        /// <summary>
        /// API lấy chi tiết người dùng theo ID
        /// </summary>
        [HttpGet("/api/users/{id}")]
        public IActionResult ApiUsersGetById(long id)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT id, name, email, phone, role, status, created_at, updated_at FROM users WHERE id=@id",
                conn
            );

            cmd.Parameters.AddWithValue("@id", id);

            using var r = cmd.ExecuteReader();

            if (!r.Read()) return NotFound(new { message = "not_found" });

            return Ok(new
            {
                Id = r.GetInt64(0),
                Name = r.GetString(1),
                Email = r.GetString(2),
                Phone = r.IsDBNull(3) ? "" : r.GetString(3),
                Role = r.GetString(4),
                Status = r.GetInt16(5),
                CreatedAt = r.GetDateTime(6),
                UpdatedAt = r.GetDateTime(7)
            });
        }

        /// <summary>
        /// API tạo người dùng mới
        /// </summary>
        [HttpPost("/api/users")]
        public IActionResult ApiUsersCreate([FromBody] UserManageRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name) ||
                string.IsNullOrWhiteSpace(req.Email) ||
                string.IsNullOrWhiteSpace(req.Password))
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

            using var cmd = new NpgsqlCommand(
                @"INSERT INTO users(name,email,phone,password,avatar,role,status)
                  VALUES(@n,@e,@p,@pw,'default.png',@r,@st) RETURNING id",
                conn
            );

            cmd.Parameters.AddWithValue("@n", req.Name);
            cmd.Parameters.AddWithValue("@e", req.Email);
            cmd.Parameters.AddWithValue("@p", (object?)req.Phone ?? "");
            cmd.Parameters.AddWithValue("@pw", hashed);
            cmd.Parameters.AddWithValue("@r", role);
            cmd.Parameters.AddWithValue("@st", req.Status ?? 1);

            var id = (long)cmd.ExecuteScalar();

            return Ok(new { id });
        }

        /// <summary>
        /// API cập nhật người dùng theo ID
        /// </summary>
        [HttpPut("/api/users/{id}")]
        public IActionResult ApiUsersUpdate(long id, [FromBody] UserManageRequest req)
        {
            if (string.IsNullOrWhiteSpace(req.Name) ||
                string.IsNullOrWhiteSpace(req.Email))
                return BadRequest(new { message = "validation_failed" });

            var role = string.IsNullOrWhiteSpace(req.Role) ? "user" : req.Role.Trim().ToLower();
            if (role != "user" && role != "admin") return BadRequest(new { message = "invalid_role" });

            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using (var dup = new NpgsqlCommand(
                "SELECT COUNT(*) FROM users WHERE email=@e AND id<>@id",
                conn))
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
            cmd.Parameters.AddWithValue("@p", (object?)req.Phone ?? "");
            cmd.Parameters.AddWithValue("@r", role);
            cmd.Parameters.AddWithValue("@st", req.Status ?? 1);

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

        /// <summary>
        /// API bật/tắt trạng thái người dùng
        /// </summary>
        [HttpPatch("/api/users/{id}/status")]
        public IActionResult ApiUsersToggleStatus(long id)
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

        /// <summary>
        /// API xóa người dùng
        /// </summary>
        [HttpDelete("/api/users/{id}")]
        public IActionResult ApiUsersDelete(long id)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand("DELETE FROM users WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            if (rows == 0) return NotFound(new { message = "not_found" });

            return Ok(new { id });
        }

        #endregion

        // ============================================================
        // =========  🔵  REGION 2.5: API USER ADMIN CHECK  ==========
        // ============================================================

        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("/api/user/is-admin")]
        public IActionResult IsAdmin()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email)) return Unauthorized(new { isAdmin = false });

            using var conn = new Npgsql.NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            using var cmd = new Npgsql.NpgsqlCommand("SELECT role FROM users WHERE email=@e LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@e", email);
            var roleObj = cmd.ExecuteScalar();
            var isAdmin = false;
            if (roleObj != null)
            {
                if (roleObj is short s) isAdmin = s == 1;
                else if (roleObj is int i) isAdmin = i == 1;
                else if (roleObj is string rs) isAdmin = rs.Trim().ToLowerInvariant() == "admin" || rs.Trim() == "1";
            }
            return Ok(new { isAdmin });
        }

        // ============================================================
        // =========  🔵  REGION 3: API USER (JWT PROTECTED)  ==========
        // ============================================================

        #region API USER (JWT) — /api/user/[action]

        /// <summary>
        /// API lấy thông tin user dựa trên JWT token
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet]
        [Route("/api/user/GetProfile")]
        public IActionResult GetProfile()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new { code = "unauthorized" });

            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "SELECT name, email, phone, avatar FROM users WHERE email=@e",
                conn
            );

            cmd.Parameters.AddWithValue("@e", email);

            using var reader = cmd.ExecuteReader();

            if (!reader.Read()) return NotFound(new { code = "email_not_found" });

            return Ok(new
            {
                name = reader.GetString(0),
                email,
                phone = reader.IsDBNull(2) ? "" : reader.GetString(2),
                avatar = reader.IsDBNull(3) ? "" : reader.GetString(3)
            });
        }

        /// <summary>
        /// API cập nhật profile cá nhân dựa theo JWT
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost]
        [Route("/api/user/UpdateProfile")]
        public IActionResult UpdateProfileJwt([FromBody] UpdateProfileRequest req)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new { code = "unauthorized" });

            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { code = "validation_fill_all" });

            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand(
                "UPDATE users SET name=@n, phone=@p, updated_at=NOW() WHERE email=@e",
                conn
            );

            cmd.Parameters.AddWithValue("@n", req.Name);
            cmd.Parameters.AddWithValue("@p", (object?)req.Phone ?? "");
            cmd.Parameters.AddWithValue("@e", email);

            var rows = cmd.ExecuteNonQuery();
            if (rows == 0) return NotFound(new { code = "email_not_found" });

            return Ok(new { code = "update_profile_success" });
        }

        /// <summary>
        /// API đổi mật khẩu người dùng (JWT)
        /// </summary>
        [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
        [HttpPost]
        [Route("/api/user/ChangePassword")]
        public IActionResult ChangePassword([FromBody] ChangePasswordRequest req)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new { code = "unauthorized" });

            if (string.IsNullOrWhiteSpace(req.CurrentPassword) ||
                string.IsNullOrWhiteSpace(req.NewPassword))
                return BadRequest(new { code = "validation_fill_all" });

            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var getCmd = new NpgsqlCommand("SELECT password FROM users WHERE email=@e", conn);
            getCmd.Parameters.AddWithValue("@e", email);

            var hashed = (string?)getCmd.ExecuteScalar();
            if (string.IsNullOrEmpty(hashed)) return NotFound(new { code = "email_not_found" });

            if (!BCrypt.Net.BCrypt.Verify(req.CurrentPassword, hashed))
                return Unauthorized(new { code = "invalid_password" });

            var newHash = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);

            using var upd = new NpgsqlCommand(
                "UPDATE users SET password=@pw, updated_at=NOW() WHERE email=@e",
                conn
            );

            upd.Parameters.AddWithValue("@pw", newHash);
            upd.Parameters.AddWithValue("@e", email);
            upd.ExecuteNonQuery();

            return Ok(new { code = "password_change_success" });
        }

        #endregion
    }

    // ============================================================
    // ====================  🔵 DTO CLASSES ========================
    // ============================================================

    public class UserManageRequest
    {
        public string Name { get; set; } = "";
        public string Email { get; set; } = "";
        public string? Phone { get; set; }
        public string? Password { get; set; }
        public string Role { get; set; } = "user";
        public int? Status { get; set; } = 1;
    }

    public class UpdateProfileRequest
    {
        public string Name { get; set; } = "";
        public string? Phone { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = "";
        public string NewPassword { get; set; } = "";
    }
}
