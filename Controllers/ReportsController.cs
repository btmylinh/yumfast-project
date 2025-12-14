using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/reports")] 
    public class ReportsController : ControllerBase
    {
        private readonly IConfiguration _config;

        public ReportsController(IConfiguration config)
        {
            _config = config;
        }

        // ------------------------------------------------------------------
        // API: Báo cáo theo ngày trong 1 tháng
        // GET /api/reports/daily?month=2025-01
        //
        // Nếu không truyền month → tự động lấy tháng hiện tại.
        // ------------------------------------------------------------------
        [HttpGet("daily")]
        public IActionResult Daily([FromQuery] string? month)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            // Xác định ngày bắt đầu và kết thúc của tháng
            DateTime start;
            if (!string.IsNullOrWhiteSpace(month) && DateTime.TryParse(month + "-01", out var m))
            {
                start = new DateTime(m.Year, m.Month, 1);
            }
            else
            {
                start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            }

            var end = start.AddMonths(1); // cuối tháng

            // Map chứa dữ liệu của từng ngày
            var map = new Dictionary<DateTime, DailyReportDto>();

            // -----------------------------------------------------------
            // 1) Lấy số đơn hàng, doanh thu, số khách hàng trong từng ngày
            // -----------------------------------------------------------
            using (var cmd = new NpgsqlCommand(@"
                SELECT 
                    date_trunc('day', o.created_at)::date AS d,
                    COUNT(*) AS orders_count,
                    SUM(CASE WHEN o.payment_status = 1 THEN o.total_price ELSE 0 END) AS revenue_total,
                    COUNT(DISTINCT o.user_id) AS customers
                FROM orders o
                WHERE o.created_at >= @s AND o.created_at < @e
                GROUP BY d
                ORDER BY d", conn))
            {
                cmd.Parameters.AddWithValue("@s", start);
                cmd.Parameters.AddWithValue("@e", end);

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var date = r.GetDateTime(0).Date;

                    map[date] = new DailyReportDto
                    {
                        ReportDate = date,
                        OrdersCount = (long)r.GetInt64(1),
                        RevenueTotal = r.IsDBNull(2) ? 0 : r.GetInt64(2),
                        UsersCount = (long)r.GetInt64(3),
                        ProductsSold = 0,   // sẽ cập nhật ở truy vấn tiếp theo
                        Status = 1
                    };
                }
            }

            // -----------------------------------------------------------
            // 2) Lấy tổng số lượng sản phẩm bán ra theo từng ngày
            // -----------------------------------------------------------
            using (var cmd = new NpgsqlCommand(@"
                SELECT 
                    date_trunc('day', o.created_at)::date AS d,
                    SUM(oi.quantity) AS qty
                FROM order_items oi
                JOIN orders o ON oi.order_id = o.id
                WHERE o.created_at >= @s AND o.created_at < @e
                GROUP BY d
                ORDER BY d", conn))
            {
                cmd.Parameters.AddWithValue("@s", start);
                cmd.Parameters.AddWithValue("@e", end);

                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var date = r.GetDateTime(0).Date;

                    if (!map.TryGetValue(date, out var item))
                    {
                        item = new DailyReportDto { ReportDate = date, Status = 1 };
                        map[date] = item;
                    }

                    item.ProductsSold = r.IsDBNull(1) ? 0 : r.GetInt64(1);
                }
            }

            // Trả về báo cáo đã sắp xếp theo ngày
            var list = map.Values.OrderBy(x => x.ReportDate).ToList();
            return Ok(new { data = list });
        }
    }

    // ------------------------------------------------------------------
    // DTO chứa dữ liệu báo cáo từng ngày
    // ------------------------------------------------------------------
    public class DailyReportDto
    {
        public DateTime ReportDate { get; set; }  // Ngày báo cáo
        public long OrdersCount { get; set; }     // Số đơn hàng
        public long RevenueTotal { get; set; }    // Tổng doanh thu (đơn đã thanh toán)
        public long UsersCount { get; set; }      // Số lượng khách hàng trong ngày
        public long ProductsSold { get; set; }    // Tổng sản phẩm bán ra
        public int Status { get; set; }           // Luôn 1 (cho FE dùng hiển thị)
    }
}
