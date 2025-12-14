using Microsoft.Extensions.Configuration;
using Npgsql;

namespace WebApp.Services
{
    public interface IChatService
    {
        object Chat(string text);
    }

    public class ChatService : IChatService
    {
        private readonly IConfiguration _config;

        public ChatService(IConfiguration config)
        {
            _config = config;
        }

        // ============================================================
        //  Hàm chính xử lý yêu cầu chat
        // ============================================================
        public object Chat(string text)
        {
            var q = (text ?? string.Empty).Trim();
            if (q.Length == 0)
            {
                return new
                {
                    reply = "Bạn muốn tìm sản phẩm gì? (ví dụ: burger, gà rán)"
                };
            }

            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            // Phát hiện intent coupon
            var lower = q.ToLower();
            var wantCoupons =
                lower.Contains("mã") ||
                lower.Contains("coupon") ||
                lower.Contains("giảm");

            // ============================================================
            //  XỬ LÝ PHÂN TÍCH KHOẢNG GIÁ
            // ============================================================
            decimal? minPrice = null;
            decimal? maxPrice = null;

            var normalized = lower.Replace("đến", "-")
                                  .Replace("tới", "-")
                                  .Replace("to", "-");

            var hasDuoi = normalized.Contains("dưới") || normalized.Contains("<=") || normalized.Contains("< ");
            var hasTren = normalized.Contains("trên") || normalized.Contains(">=") || normalized.Contains("> ");

            var factor = DetectPriceFactor(normalized);

            // Lấy toàn bộ số (hoặc khoảng giá)
            string digits = new string(normalized.Where(c => char.IsDigit(c) || c == '-').ToArray());

            ParsePriceRange(digits, hasDuoi, hasTren, factor, ref minPrice, ref maxPrice);

            AdjustUnitIfMissing(ref minPrice, ref maxPrice, factor);

            // ============================================================
            //  TÌM KEYWORD SẢN PHẨM
            // ============================================================
            var lettersOnly = new string(lower.Where(c => char.IsLetter(c) || char.IsWhiteSpace(c)).ToArray());
            var kw = lettersOnly
                .Replace("duoi", "")
                .Replace("dưới", "")
                .Replace("tren", "")
                .Replace("trên", "")
                .Replace("den", "")
                .Replace("đến", "")
                .Replace("toi", "")
                .Replace("tới", "")
                .Replace("gia", "")
                .Trim();

            var hasWord = kw.Any(ch => char.IsLetter(ch));

            // ============================================================
            //  TRUY VẤN SẢN PHẨM
            // ============================================================
            var items = QueryProducts(conn, kw, hasWord, minPrice, maxPrice);

            // ============================================================
            //  TRUY VẤN MÃ GIẢM GIÁ
            // ============================================================
            var coupons = wantCoupons ? QueryCoupons(conn) : new List<object>();

            // ============================================================
            //  XỬ LÝ TRẢ VỀ
            // ============================================================
            if (items.Count == 0 && coupons.Count == 0)
            {
                return new
                {
                    reply = "Mình chưa tìm thấy kết quả. Bạn có thể thử gõ tên sản phẩm hoặc thêm khoảng giá (ví dụ: burger 30-60, gà dưới 50)."
                };
            }

            var reply = wantCoupons
                ? "Mình tìm được một số mã giảm giá và sản phẩm liên quan. Bạn muốn xem chi tiết hoặc áp dụng mã?"
                : "Mình gợi ý một vài sản phẩm phù hợp. Bạn muốn xem chi tiết hoặc thêm vào giỏ?";

            return new { reply, items, coupons };
        }

        // ============================================================
        //  HELPER: Xác định đơn vị giá (k, nghìn, triệu…)
        // ============================================================
        private decimal DetectPriceFactor(string normalized)
        {
            if (normalized.Contains("k") || normalized.Contains("nghìn") || normalized.Contains("nghin"))
                return 1000m;

            if (normalized.Contains("triệu") || normalized.Contains("m"))
                return 1_000_000m;

            return 1m;
        }

        // ============================================================
        //  HELPER: Phân tích khoảng giá trong câu
        // ============================================================
        private void ParsePriceRange(
            string digits,
            bool hasDuoi,
            bool hasTren,
            decimal factor,
            ref decimal? minPrice,
            ref decimal? maxPrice)
        {
            if (string.IsNullOrEmpty(digits))
                return;

            var parts = digits.Split('-', StringSplitOptions.RemoveEmptyEntries);

            // Xử lý dạng 30-70
            if (parts.Length == 2)
            {
                if (decimal.TryParse(parts[0], out var a) && decimal.TryParse(parts[1], out var b))
                {
                    minPrice = Math.Min(a, b) * factor;
                    maxPrice = Math.Max(a, b) * factor;
                }
                return;
            }

            // Xử lý 1 số (dưới 50, trên 30…)
            if (parts.Length == 1)
            {
                if (!decimal.TryParse(parts[0], out var v))
                    return;

                if (hasDuoi)
                    maxPrice = v * factor;
                else if (hasTren)
                    minPrice = v * factor;
                else
                    maxPrice = v * factor;
            }
        }

        // ============================================================
        //  HELPER: Điều chỉnh đơn vị khi người dùng quên "k" / triệu
        // ============================================================
        private void AdjustUnitIfMissing(ref decimal? minPrice, ref decimal? maxPrice, decimal factor)
        {
            if (factor != 1m)
                return;

            if (minPrice.HasValue && minPrice.Value < 2000)
                minPrice *= 1000;

            if (maxPrice.HasValue && maxPrice.Value < 2000)
                maxPrice *= 1000;
        }

        // ============================================================
        //  HELPER: Query sản phẩm theo keyword + giá
        // ============================================================
        private List<object> QueryProducts(
            NpgsqlConnection conn,
            string kw,
            bool hasWord,
            decimal? minPrice,
            decimal? maxPrice)
        {
            var items = new List<object>();

            var sql = "SELECT id, name, price FROM products WHERE LOWER(name) ILIKE @kw";

            if (minPrice.HasValue && maxPrice.HasValue)
                sql += " AND price BETWEEN @min AND @max";
            else if (minPrice.HasValue)
                sql += " AND price >= @min";
            else if (maxPrice.HasValue)
                sql += " AND price <= @max";

            sql += " ORDER BY created_at DESC LIMIT 5";

            using var cmd = new NpgsqlCommand(sql, conn);

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

            return items;
        }

        // ============================================================
        //  HELPER: Query mã giảm giá hoạt động
        // ============================================================
        private List<object> QueryCoupons(NpgsqlConnection conn)
        {
            var coupons = new List<object>();

            using var cmd = new NpgsqlCommand(@"
                SELECT id, code, name, type, value, total, used_count
                FROM coupons
                WHERE status=1 AND start_at<=NOW() AND end_at>NOW() AND used_count < total
                ORDER BY start_at DESC LIMIT 5", conn);

            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                var total = r.GetInt32(5);
                var used = r.GetInt32(6);

                coupons.Add(new
                {
                    id = r.GetInt64(0),
                    code = r.GetString(1),
                    name = r.GetString(2),
                    type = r.GetInt16(3),
                    value = r.GetInt32(4),
                    remaining = total - used
                });
            }

            return coupons;
        }
    }
}
