using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/reports")]
    public class ReportsApiController : ControllerBase
    {
        private readonly IConfiguration _config;
        public ReportsApiController(IConfiguration config) { _config = config; }

        [HttpGet("daily")]
        public IActionResult Daily([FromQuery] string? month)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            DateTime start;
            if (!string.IsNullOrWhiteSpace(month) && DateTime.TryParse(month + "-01", out var m)) start = new DateTime(m.Year, m.Month, 1);
            else start = new DateTime(DateTime.Now.Year, DateTime.Now.Month, 1);
            var end = start.AddMonths(1);

            var map = new Dictionary<DateTime, DailyReportDto>();

            using (var cmd = new NpgsqlCommand(@"SELECT date_trunc('day', o.created_at)::date AS d,
                                                      COUNT(*) AS orders_count,
                                                      SUM(CASE WHEN o.payment_status=1 THEN o.total_price ELSE 0 END) AS revenue_total,
                                                      COUNT(DISTINCT o.user_id) AS customers
                                               FROM orders o
                                               WHERE o.created_at>=@s AND o.created_at<@e
                                               GROUP BY d
                                               ORDER BY d", conn))
            {
                cmd.Parameters.AddWithValue("@s", start);
                cmd.Parameters.AddWithValue("@e", end);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var d = r.GetDateTime(0).Date;
                    map[d] = new DailyReportDto
                    {
                        ReportDate = d,
                        OrdersCount = (long)r.GetInt64(1),
                        RevenueTotal = r.IsDBNull(2) ? 0 : (long)r.GetInt64(2),
                        UsersCount = (long)r.GetInt64(3),
                        ProductsSold = 0,
                        Status = 1
                    };
                }
            }

            using (var cmd = new NpgsqlCommand(@"SELECT date_trunc('day', o.created_at)::date AS d,
                                                      SUM(oi.quantity) AS qty
                                               FROM order_items oi
                                               JOIN orders o ON oi.order_id = o.id
                                               WHERE o.created_at>=@s AND o.created_at<@e
                                               GROUP BY d
                                               ORDER BY d", conn))
            {
                cmd.Parameters.AddWithValue("@s", start);
                cmd.Parameters.AddWithValue("@e", end);
                using var r = cmd.ExecuteReader();
                while (r.Read())
                {
                    var d = r.GetDateTime(0).Date;
                    if (!map.TryGetValue(d, out var item))
                    {
                        item = new DailyReportDto { ReportDate = d, Status = 1 };
                        map[d] = item;
                    }
                    item.ProductsSold = r.IsDBNull(1) ? 0 : (long)r.GetInt64(1);
                }
            }

            var list = map.Values.OrderBy(x => x.ReportDate).ToList();
            return Ok(new { data = list });
        }
    }

    public class DailyReportDto
    {
        public DateTime ReportDate { get; set; }
        public long OrdersCount { get; set; }
        public long RevenueTotal { get; set; }
        public long UsersCount { get; set; }
        public long ProductsSold { get; set; }
        public int Status { get; set; }
    }
}
