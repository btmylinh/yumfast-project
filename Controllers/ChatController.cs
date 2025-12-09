using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Linq;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/chat")]
    public class ChatController : ControllerBase
    {
        private readonly IConfiguration _config;
        public ChatController(IConfiguration config) { _config = config; }

        [HttpPost]
        public IActionResult Chat([FromBody] ChatRequest req)
        {
            var q = (req.Text ?? string.Empty).Trim();
            if (q.Length == 0) return Ok(new { reply = "Bạn muốn tìm sản phẩm gì? (ví dụ: burger, gà rán)" });

            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            // Simple intents
            var lower = q.ToLower();
            var wantCoupons = lower.Contains("mã") || lower.Contains("coupon") || lower.Contains("giảm");
            var items = new List<object>();

            decimal? minPrice = null;
            decimal? maxPrice = null;
            var normalized = lower.Replace("đến", "-").Replace("tới", "-").Replace("to", "-");
            var hasDuoi = normalized.Contains("dưới") || normalized.Contains("<=") || normalized.Contains("< ");
            var hasTren = normalized.Contains("trên") || normalized.Contains(">=") || normalized.Contains("> ");
            var factor = 1m;
            if (normalized.Contains("k") || normalized.Contains("nghìn") || normalized.Contains("nghin")) factor = 1000m;
            else if (normalized.Contains("triệu") || normalized.Contains("m")) factor = 1000000m;
            string digits = new string(Array.FindAll(normalized.ToCharArray(), c => char.IsDigit(c) || c=='-' ));
            if (!string.IsNullOrEmpty(digits))
            {
                var parts = digits.Split('-', StringSplitOptions.RemoveEmptyEntries);
                if (parts.Length == 2)
                {
                    if (decimal.TryParse(parts[0], out var a) && decimal.TryParse(parts[1], out var b))
                    {
                        minPrice = Math.Min(a, b) * factor;
                        maxPrice = Math.Max(a, b) * factor;
                    }
                }
                else if (parts.Length == 1)
                {
                    if (decimal.TryParse(parts[0], out var v))
                    {
                        if (hasDuoi) { maxPrice = v * factor; }
                        else if (hasTren) { minPrice = v * factor; }
                        else { maxPrice = v * factor; }
                    }
                }
            }
            if (factor == 1m)
            {
                if (minPrice.HasValue && minPrice.Value > 0 && minPrice.Value < 2000) minPrice *= 1000m;
                if (maxPrice.HasValue && maxPrice.Value > 0 && maxPrice.Value < 2000) maxPrice *= 1000m;
            }

            // Search products by keyword
            var lettersOnly = new string(Array.FindAll(lower.ToCharArray(), c => char.IsLetter(c) || char.IsWhiteSpace(c)));
            var kw = lettersOnly
                .Replace("duoi", string.Empty)
                .Replace("dưới", string.Empty)
                .Replace("tren", string.Empty)
                .Replace("trên", string.Empty)
                .Replace("den", string.Empty)
                .Replace("đến", string.Empty)
                .Replace("toi", string.Empty)
                .Replace("tới", string.Empty)
                .Replace("gia", string.Empty)
                .Trim();
            var hasWord = kw.Any(ch => char.IsLetter(ch));
            var sql = "SELECT id, name, price FROM products WHERE LOWER(name) ILIKE @kw";
            if (minPrice.HasValue && maxPrice.HasValue) sql += " AND price BETWEEN @min AND @max";
            else if (minPrice.HasValue) sql += " AND price >= @min";
            else if (maxPrice.HasValue) sql += " AND price <= @max";
            sql += " ORDER BY created_at DESC LIMIT 5";
            using (var cmd = new NpgsqlCommand(sql, conn))
            {
                cmd.Parameters.AddWithValue("@kw", hasWord ? "%" + kw + "%" : "%");
                if (minPrice.HasValue) cmd.Parameters.AddWithValue("@min", minPrice.Value);
                if (maxPrice.HasValue) cmd.Parameters.AddWithValue("@max", maxPrice.Value);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    items.Add(new
                    {
                        id = r.GetInt64(0),
                        name = r.GetString(1),
                        price = r.GetInt32(2)
                    });
                }
            }

            // Collect active coupons optionally
            var coupons = new List<object>();
            if (wantCoupons)
            {
                using var ccmd = new NpgsqlCommand(@"SELECT id, code, name, type, value, total, used_count
                                                     FROM coupons
                                                     WHERE status=1 AND start_at<=NOW() AND end_at>NOW() AND used_count < total
                                                     ORDER BY start_at DESC LIMIT 5", conn);
                using var cr = ccmd.ExecuteReader();
                while (cr.Read())
                {
                    var total = cr.GetInt32(5);
                    var used = cr.GetInt32(6);
                    coupons.Add(new
                    {
                        id = cr.GetInt64(0),
                        code = cr.GetString(1),
                        name = cr.GetString(2),
                        type = cr.GetInt16(3),
                        value = cr.GetInt32(4),
                        remaining = total - used
                    });
                }
            }

            if (items.Count == 0 && coupons.Count == 0)
            {
                return Ok(new { reply = "Mình chưa tìm thấy kết quả. Bạn có thể thử gõ tên sản phẩm hoặc thêm khoảng giá (ví dụ: burger 30-60, gà dưới 50)." });
            }

            var reply = wantCoupons
                ? "Mình tìm được một số mã giảm giá và sản phẩm liên quan. Bạn muốn xem chi tiết hoặc áp dụng mã?"
                : "Mình gợi ý một vài sản phẩm phù hợp. Bạn muốn xem chi tiết hoặc thêm vào giỏ?";

            return Ok(new { reply, items, coupons });
        }
    }

    public class ChatRequest
    {
        public string? Text { get; set; }
    }
}
