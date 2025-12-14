using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/auth")]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;

        public AuthController(IConfiguration config)
        {
            _config = config;
        }

        [HttpGet("status")]
        public IActionResult Status()
        {
            return Ok(new { authenticated = false });
        }

        // Login API: POST /api/auth/login
        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Password))
                return BadRequest(new { code = "validation_fill_all" });

            var connStr = _config.GetConnectionString("DefaultConnection");
            using var conn = new NpgsqlConnection(connStr);
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT id, name, email, password, status, role FROM users WHERE email=@e LIMIT 1", conn);
            cmd.Parameters.AddWithValue("@e", req.Email);

            using var r = cmd.ExecuteReader();
            if (!r.Read())
            {
                return Unauthorized(new { code = "email_not_found" });
            }

            var id = r.GetInt64(0);
            var name = r.GetString(1);
            var email = r.GetString(2);
            var hashed = r.IsDBNull(3) ? string.Empty : r.GetString(3);
            var status = r.IsDBNull(4) ? 1 : r.GetInt16(4);
            var role = r.IsDBNull(5) ? "user" : r.GetString(5);

            if (status != 1)
            {
                return Unauthorized(new { code = "account_inactive" });
            }

            if (string.IsNullOrEmpty(hashed) || !BCrypt.Net.BCrypt.Verify(req.Password, hashed))
            {
                return Unauthorized(new { code = "invalid_password" });
            }

            // Issue JWT token
            var token = GenerateJwtToken(id, name, email, role);
            return Ok(new { code = "signin_success", token, name, email, role });
        }

        // Mirror route: POST /auth/Login (for existing scripts)
        [HttpPost("/auth/Login")]
        public IActionResult LoginMirror([FromBody] LoginRequest req) => Login(req);

        private string GenerateJwtToken(long userId, string name, string email, string role)
        {
            var jwtKey = _config["Jwt:Key"];
            var jwtIssuer = _config["Jwt:Issuer"];
            var jwtAudience = _config["Jwt:Audience"];

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new List<Claim>
            {
                new Claim(JwtRegisteredClaimNames.Sub, email),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim(ClaimTypes.Email, email),
                new Claim(ClaimTypes.Name, name ?? string.Empty),
                new Claim("uid", userId.ToString()),
                new Claim(ClaimTypes.Role, role)
            };

            var token = new JwtSecurityToken(
                issuer: jwtIssuer,
                audience: jwtAudience,
                claims: claims,
                expires: DateTime.UtcNow.AddDays(7),
                signingCredentials: creds);

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    public class LoginRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
