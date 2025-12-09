using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.IdentityModel.Tokens;
using BCrypt.Net;
using WebApp.Services;

namespace WebApp.Controllers
{
    // API Controller cho các endpoint auth
    [Route("api/[controller]/[action]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IEmailSender _email;

    // Bộ nhớ tạm lưu OTP
        private static readonly Dictionary<string, (string Code, DateTime Expire)> _otpStore = new();
    // Bộ nhớ tạm lưu OTP quên mật khẩu
    private static readonly Dictionary<string, (string Code, DateTime Expire)> _resetOtpStore = new();

        public AuthController(IConfiguration config, IEmailSender email)
        {
            _config = config;
            _email = email;
        }

    // ĐĂNG KÝ + GỬI OTP QUA EMAIL
    [HttpPost]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                // Validate input trước
                if (string.IsNullOrWhiteSpace(request.Name) ||
                    string.IsNullOrWhiteSpace(request.Email) ||
                    string.IsNullOrWhiteSpace(request.Password))
                {
                    return BadRequest(new { error = "validation_fill_all" });
                }

                // Kiểm tra độ mạnh mật khẩu phía server
                if (!IsStrongPassword(request.Password))
                {
                    return BadRequest(new { error = "weak_password" });
                }

                using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Kiểm tra email trùng trước khi INSERT
                using (var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE email=@e", conn))
                {
                    checkCmd.Parameters.AddWithValue("@e", request.Email);
                    var count = (long)checkCmd.ExecuteScalar();
                    if (count > 0)
                        return StatusCode(409, new { error = "email_exists" });
                }

                // Băm mật khẩu
                var hashed = BCrypt.Net.BCrypt.HashPassword(request.Password);

                // Tạo user unverified chỉ khi hợp lệ (status = 0 là chưa xác minh, 1 là đã xác minh)
                long userId;
                using (var cmd = new NpgsqlCommand(@"
                    INSERT INTO users (name, email, phone, password, avatar, role, status)
                    VALUES (@n, @e, @p, @pw, @av, 'user', 0)
                    RETURNING id", conn))
                {
                    cmd.Parameters.AddWithValue("@n", request.Name);
                    cmd.Parameters.AddWithValue("@e", request.Email);
                    cmd.Parameters.AddWithValue("@p", (object?)request.Phone ?? "");
                    cmd.Parameters.AddWithValue("@pw", hashed);
                    cmd.Parameters.AddWithValue("@av", "default.png");
                    userId = (long)cmd.ExecuteScalar();
                }

                // Sinh mã OTP
                var otp = new Random().Next(100000, 999999).ToString();

                // Lưu vào bộ nhớ (RAM) với hạn 3 phút
                _otpStore[request.Email] = (otp, DateTime.UtcNow.AddMinutes(3));

                // Gửi email OTP 
                try
                {
                    var html = $@"
                        <p>Chào {request.Name},</p>
                        <p>Mã xác minh email của bạn là <b style='font-size:18px'>{otp}</b></p>
                        <p>Mã có hiệu lực trong 3 phút.</p>";
                    await _email.SendAsync(request.Email, "[YumFast] Xác minh email", html);
                }
                catch (Exception mailEx)
                {
                    Console.WriteLine($"[EMAIL ERROR] {mailEx}");
                }

                // Trả về thành công
                return Ok(new { success = true });
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"[Postgres ERROR] {ex.MessageText}");
                return StatusCode(500, new { error = "internal_server_error" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER ERROR] {ex}");
                return StatusCode(500, new { error = "internal_server_error" });
            }
        }

        private static bool IsStrongPassword(string password)
        {
            if (string.IsNullOrEmpty(password) || password.Length < 8) return false;
            var hasLower = password.Any(char.IsLower);
            var hasUpper = password.Any(char.IsUpper);
            var hasDigit = password.Any(char.IsDigit);
            var hasSpecial = password.Any(c => !char.IsLetterOrDigit(c));
            return hasLower && hasUpper && hasDigit && hasSpecial;
        }

    // XÁC MINH OTP
    [HttpPost]
        public IActionResult VerifyOtp([FromBody] VerifyOtpRequest req)
        {
            try
            {
                if (!_otpStore.TryGetValue(req.Email, out var entry) || DateTime.UtcNow > entry.Expire || req.Code != entry.Code)
                {
                    _otpStore.Remove(req.Email); // Xóa luôn nếu sai hoặc hết hạn
                    return BadRequest(new { code = "otp_invalid_or_expired" });
                }

                // Đúng OTP, cập nhật status = 1 
                using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();
                using var cmd = new NpgsqlCommand("UPDATE users SET status=1 WHERE email=@e", conn);
                cmd.Parameters.AddWithValue("@e", req.Email);
                cmd.ExecuteNonQuery();

                _otpStore.Remove(req.Email);
                return Ok(new { code = "verify_success" });
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"[Postgres ERROR] {ex.MessageText}");
                return StatusCode(500, new { message = "Lỗi cơ sở dữ liệu!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER ERROR] {ex}");
                return StatusCode(500, new { message = "Lỗi máy chủ!" });
            }
        }

    // GỬI LẠI OTP
    [HttpPost]
        public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest req)
        {
            var otp = new Random().Next(100000, 999999).ToString();
            _otpStore[req.Email] = (otp, DateTime.UtcNow.AddMinutes(3));

            var html = $@"
                <p>Mã OTP mới của bạn là <b>{otp}</b></p>
                <p>Mã có hiệu lực trong 3 phút.</p>";
            await _email.SendAsync(req.Email, "[YumFast] OTP mới", html);

            return Ok(new { code = "otp_resent" });
        }

    // QUÊN MẬT KHẨU - GỬI OTP
    [HttpPost]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Email))
                    return BadRequest(new { code = "validation_enter_email" });

                using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();
                using (var check = new NpgsqlCommand("SELECT COUNT(*) FROM users WHERE email=@e", conn))
                {
                    check.Parameters.AddWithValue("@e", req.Email);
                    var exists = (long)check.ExecuteScalar() > 0;
                    if (!exists)
                        return NotFound(new { code = "email_not_found" });
                }

                var otp = new Random().Next(100000, 999999).ToString();
                _resetOtpStore[req.Email] = (otp, DateTime.UtcNow.AddMinutes(3));

                try
                {
                    var html = $@"<p>Mã OTP đặt lại mật khẩu của bạn là <b style='font-size:18px'>{otp}</b></p><p>Mã có hiệu lực trong 3 phút.</p>";
                    await _email.SendAsync(req.Email, "[YumFast] OTP đặt lại mật khẩu", html);
                }
                catch (Exception mailEx)
                {
                    Console.WriteLine($"[EMAIL ERROR] {mailEx}");
                }

                return Ok(new { code = "otp_sent" });
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"[Postgres ERROR] {ex.MessageText}");
                return StatusCode(500, new { message = "Lỗi cơ sở dữ liệu!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER ERROR] {ex}");
                return StatusCode(500, new { message = "Lỗi máy chủ!" });
            }
        }

    // ĐẶT LẠI MẬT KHẨU
    [HttpPost]
        public IActionResult ResetPassword([FromBody] ResetPasswordRequest req)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(req.Email) || string.IsNullOrWhiteSpace(req.Otp) || string.IsNullOrWhiteSpace(req.NewPassword))
                    return BadRequest(new { code = "validation_fill_all" });

                if (!_resetOtpStore.TryGetValue(req.Email, out var entry) || DateTime.UtcNow > entry.Expire || req.Otp != entry.Code)
                {
                    _resetOtpStore.Remove(req.Email);
                    return BadRequest(new { code = "otp_invalid_or_expired" });
                }

                // Hợp lệ: cập nhật mật khẩu
                var hashed = BCrypt.Net.BCrypt.HashPassword(req.NewPassword);
                using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();
                using var cmd = new NpgsqlCommand("UPDATE users SET password=@pw WHERE email=@e", conn);
                cmd.Parameters.AddWithValue("@pw", hashed);
                cmd.Parameters.AddWithValue("@e", req.Email);
                cmd.ExecuteNonQuery();

                _resetOtpStore.Remove(req.Email);
                return Ok(new { code = "reset_success" });
            }
            catch (PostgresException ex)
            {
                Console.WriteLine($"[Postgres ERROR] {ex.MessageText}");
                return StatusCode(500, new { message = "Lỗi cơ sở dữ liệu!" });
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[SERVER ERROR] {ex}");
                return StatusCode(500, new { message = "Lỗi máy chủ!" });
            }
        }

    // ĐĂNG NHẬP 
    [HttpPost]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand("SELECT id, name, email, password, status FROM users WHERE email=@e", conn);
            cmd.Parameters.AddWithValue("@e", request.Email);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return Unauthorized(new { code = "email_not_found" });

            var id = reader.GetInt64(0);
            var name = reader.GetString(1);
            var email = reader.GetString(2);
            var hashedPassword = reader.GetString(3);
            var status = reader.GetInt16(4);


            if (status == 0)
                return Unauthorized(new { code = "account_unverified" });

            if (!BCrypt.Net.BCrypt.Verify(request.Password, hashedPassword))
                return Unauthorized(new { code = "invalid_password" });

            var token = GenerateJwtToken(id, email, name);
            return Ok(new { code = "signin_success", token });
        }

    // HÀM TẠO JWT
        private string GenerateJwtToken(long id, string email, string name)
        {
            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(_config["Jwt:Key"]));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, id.ToString()),
                new Claim(JwtRegisteredClaimNames.Email, email),
                new Claim("name", name),
                new Claim("role", "user")
            };

            var token = new JwtSecurityToken(
                issuer: _config["Jwt:Issuer"],
                audience: _config["Jwt:Audience"],
                claims: claims,
                expires: DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:ExpireMinutes"] ?? "120")),
                signingCredentials: creds
            );

            return new JwtSecurityTokenHandler().WriteToken(token);
        }
    }

    // DTO CLASSES
    public class RegisterRequest
    {
        public string Name { get; set; } = null!;
        public string Email { get; set; } = null!;
        public string? Phone { get; set; }
        public string Password { get; set; } = null!;
    }

    public class LoginRequest
    {
        public string Email { get; set; } = null!;
        public string Password { get; set; } = null!;
    }

    public class VerifyOtpRequest
    {
        public string Email { get; set; } = null!;
        public string Code { get; set; } = null!;
    }

    public class ResendOtpRequest
    {
        public string Email { get; set; } = null!;
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = null!;
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; } = null!;
        public string Otp { get; set; } = null!;
        public string NewPassword { get; set; } = null!;
    }

    // View Controller cho các trang auth
    [Route("auth/[action]")]
    public class AuthViewController : BaseController
    {
        public AuthViewController(IJsonLocalizationService localizationService) : base(localizationService) {}

    [HttpGet]
        public IActionResult SignUp()
        {
            return View("~/Views/Auth/SignUp.cshtml");
        }

    [HttpGet]
        public IActionResult SignIn()
        {
            return View("~/Views/Auth/SignIn.cshtml");
        }

    [HttpGet]
        public IActionResult Verify()
        {
            return View("~/Views/Auth/Verify.cshtml");
        }

    [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View("~/Views/Auth/ForgotPassword.cshtml");
        }

    [HttpGet]
        public IActionResult ResetPassword()
        {
            return View("~/Views/Auth/ResetPassword.cshtml");
        }
    }
}
