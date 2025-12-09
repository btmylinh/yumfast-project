using System.Security.Claims;
using BCrypt.Net;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/user/[action]")]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class UserApiController : ControllerBase
    {
        private readonly IConfiguration _config;

        public UserApiController(IConfiguration config)
        {
            _config = config;
        }

        // GET: api/user/GetProfile
        [HttpGet]
        public IActionResult GetProfile()
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new { code = "unauthorized" });

            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT name, email, phone, avatar FROM users WHERE email=@e", conn);
            cmd.Parameters.AddWithValue("@e", email);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return NotFound(new { code = "email_not_found" });

            var name = reader.GetString(0);
            var mail = reader.GetString(1);
            var phone = reader.IsDBNull(2) ? "" : reader.GetString(2);
            var avatar = reader.IsDBNull(3) ? "" : reader.GetString(3);

            return Ok(new { name, email = mail, phone, avatar });
        }

        // POST: api/user/UpdateProfile
        [HttpPost]
        public IActionResult UpdateProfile([FromBody] UpdateProfileRequest req)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new { code = "unauthorized" });

            if (string.IsNullOrWhiteSpace(req.Name))
                return BadRequest(new { code = "validation_fill_all" });

            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            using var cmd = new NpgsqlCommand("UPDATE users SET name=@n, phone=@p, updated_at=NOW() WHERE email=@e", conn);
            cmd.Parameters.AddWithValue("@n", req.Name);
            cmd.Parameters.AddWithValue("@p", (object?)req.Phone ?? "");
            cmd.Parameters.AddWithValue("@e", email);
            var rows = cmd.ExecuteNonQuery();
            if (rows == 0) return NotFound(new { code = "email_not_found" });
            return Ok(new { code = "update_profile_success" });
        }

        // POST: api/user/ChangePassword
        [HttpPost]
        public IActionResult ChangePassword([FromBody] ChangePasswordRequest req)
        {
            var email = User.FindFirstValue(ClaimTypes.Email);
            if (string.IsNullOrEmpty(email))
                return Unauthorized(new { code = "unauthorized" });

            if (string.IsNullOrWhiteSpace(req.CurrentPassword) || string.IsNullOrWhiteSpace(req.NewPassword))
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
            using var upd = new NpgsqlCommand("UPDATE users SET password=@pw, updated_at=NOW() WHERE email=@e", conn);
            upd.Parameters.AddWithValue("@pw", newHash);
            upd.Parameters.AddWithValue("@e", email);
            upd.ExecuteNonQuery();

            return Ok(new { code = "password_change_success" });
        }
    }

    public class UpdateProfileRequest
    {
        public string Name { get; set; } = string.Empty;
        public string? Phone { get; set; }
    }

    public class ChangePasswordRequest
    {
        public string CurrentPassword { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }
}
