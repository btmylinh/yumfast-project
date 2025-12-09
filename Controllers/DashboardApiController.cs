using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/dashboard")]
    public class DashboardApiController : ControllerBase
    {
        private readonly IConfiguration _config;
        public DashboardApiController(IConfiguration config) { _config = config; }

        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var result = new DashboardStatsDto();

            using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM orders", conn))
            {
                result.TotalOrders = (long)cmd.ExecuteScalar();
            }

            using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(total_price),0) FROM orders WHERE created_at >= NOW() - INTERVAL '1 day'", conn))
            {
                result.RevenueToday = (long)(cmd.ExecuteScalar() ?? 0L);
            }
            using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(total_price),0) FROM orders WHERE created_at >= NOW() - INTERVAL '7 days'", conn))
            {
                result.Revenue7d = (long)(cmd.ExecuteScalar() ?? 0L);
            }
            using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(total_price),0) FROM orders WHERE created_at >= NOW() - INTERVAL '30 days'", conn))
            {
                result.Revenue30d = (long)(cmd.ExecuteScalar() ?? 0L);
            }

            result.RevenueByDay = new List<PointDto>();
            using (var cmd = new NpgsqlCommand(@"SELECT date_trunc('day', created_at)::date AS d, SUM(total_price) 
                                                FROM orders 
                                                WHERE created_at >= NOW() - INTERVAL '7 days' 
                                                GROUP BY d ORDER BY d", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    result.RevenueByDay.Add(new PointDto { Label = r.GetDateTime(0).ToString("yyyy-MM-dd"), Value = r.GetInt64(1) });
                }
            }

            result.RevenueByWeek = new List<PointDto>();
            using (var cmd = new NpgsqlCommand(@"SELECT date_trunc('week', created_at)::date AS w, SUM(total_price) 
                                                FROM orders 
                                                WHERE created_at >= NOW() - INTERVAL '8 weeks' 
                                                GROUP BY w ORDER BY w", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    result.RevenueByWeek.Add(new PointDto { Label = r.GetDateTime(0).ToString("yyyy-MM-dd"), Value = r.GetInt64(1) });
                }
            }

            result.TopProducts = new List<TopProductDto>();
            using (var cmd = new NpgsqlCommand(@"SELECT p.name, SUM(oi.quantity) AS qty, SUM(oi.total) AS revenue 
                                                FROM order_items oi 
                                                JOIN products p ON oi.product_id = p.id 
                                                GROUP BY p.name 
                                                ORDER BY revenue DESC 
                                                LIMIT 5", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    result.TopProducts.Add(new TopProductDto
                    {
                        Name = r.GetString(0),
                        Quantity = r.GetInt64(1),
                        Revenue = r.GetInt64(2)
                    });
                }
            }

            return Ok(new { data = result });
        }
    }

    public class DashboardStatsDto
    {
        public long TotalOrders { get; set; }
        public long RevenueToday { get; set; }
        public long Revenue7d { get; set; }
        public long Revenue30d { get; set; }
        public List<PointDto> RevenueByDay { get; set; } = new();
        public List<PointDto> RevenueByWeek { get; set; } = new();
        public List<TopProductDto> TopProducts { get; set; } = new();
    }

    public class PointDto
    {
        public string Label { get; set; } = string.Empty;
        public long Value { get; set; }
    }

    public class TopProductDto
    {
        public string Name { get; set; } = string.Empty;
        public long Quantity { get; set; }
        public long Revenue { get; set; }
    }
}
