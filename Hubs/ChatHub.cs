using Microsoft.AspNetCore.SignalR;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System.Linq;
using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace WebApp.Hubs
{
    public class ChatHub : Hub
    {
        private readonly IConfiguration _config;

        public ChatHub(IConfiguration config)
        {
            _config = config;
        }

        public async Task SendQuestion(string text)
        {
            var t = (text ?? string.Empty).Trim().ToLowerInvariant();
            var items = new List<object>();
            var coupons = new List<object>();
            var reply = string.Empty;

            try
            {
                using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                decimal? minPrice = null;
                decimal? maxPrice = null;
                var normalized = t.Replace("đến", "-").Replace("tới", "-").Replace("to", "-");
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
                // Mặc định coi số nhỏ là nghìn (vd: "gà dưới 50" => 50k)
                if (factor == 1m)
                {
                    if (minPrice.HasValue && minPrice.Value > 0 && minPrice.Value < 2000) minPrice *= 1000m;
                    if (maxPrice.HasValue && maxPrice.Value > 0 && maxPrice.Value < 2000) maxPrice *= 1000m;
                }

                // Trích xuất từ khóa chữ cái, bỏ từ khóa giá
                var lettersOnly = new string(Array.FindAll(t.ToCharArray(), c => char.IsLetter(c) || char.IsWhiteSpace(c)));
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
                    cmd.Parameters.AddWithValue("@kw", hasWord ? $"%{kw}%" : "%");
                    if (minPrice.HasValue) cmd.Parameters.AddWithValue("@min", minPrice.Value);
                    if (maxPrice.HasValue) cmd.Parameters.AddWithValue("@max", maxPrice.Value);
                    using var reader = cmd.ExecuteReader();
                    while (reader.Read())
                    {
                        items.Add(new
                        {
                            id = reader.GetInt32(0),
                            name = reader.GetString(1),
                            price = reader.GetDecimal(2)
                        });
                    }
                }

                using (var ccmd = new NpgsqlCommand("SELECT code, name, type, value, (total - used_count) AS remaining FROM coupons WHERE status = 1 AND (start_at IS NULL OR start_at <= NOW()) AND (end_at IS NULL OR end_at >= NOW()) ORDER BY created_at DESC LIMIT 5", conn))
                {
                    using var r2 = ccmd.ExecuteReader();
                    while (r2.Read())
                    {
                        coupons.Add(new
                        {
                            code = r2.GetString(0),
                            name = r2.GetString(1),
                            type = r2.GetInt32(2),
                            value = r2.GetDecimal(3),
                            remaining = r2.IsDBNull(4) ? 0 : r2.GetInt32(4)
                        });
                    }
                }

                if (items.Count > 0)
                {
                    reply = "Đây là các sản phẩm phù hợp:";
                }
                else
                {
                    reply = "Chưa tìm thấy sản phẩm khớp mô tả. Bạn thử gõ tên cụ thể hơn nhé.";
                }
            }
            catch
            {
                reply = "Lỗi kết nối máy chủ, vui lòng thử lại.";
            }

            await Clients.Caller.SendAsync("ReceiveMessage", reply, items, coupons);
        }
    }
}
