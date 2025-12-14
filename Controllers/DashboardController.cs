using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;
using WebApp.Services;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/dashboard")] // Route API: /api/dashboard
    public class DashboardController : BaseController
    {
        private readonly IConfiguration _config;

        public DashboardController(IJsonLocalizationService localizationService, IConfiguration config)
            : base(localizationService)
        {
            _config = config;
        }

        // ============================
        //         MVC VIEW
        // ============================
        // Route conventional: /dashboard/Index (Program.cs) and direct absolute routes
        [HttpGet("/dashboard")]
        [HttpGet("/dashboard/index")]
        public IActionResult Index()
        {
            return View("~/Views/Dashboard/Index.cshtml");
        }

        [HttpGet("/dashboard/banners")]
        public IActionResult Banners()
        {
            return View("~/Views/Dashboard/Banners.cshtml");
        }

        // ============================
        //         API DASHBOARD
        // ============================

        // API: Lấy số liệu thống kê Dashboard
        // GET: /api/dashboard/stats
        [HttpGet("stats")]
        public IActionResult GetStats()
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var result = new DashboardStatsDto();

            // Tổng số đơn
            using (var cmd = new NpgsqlCommand("SELECT COUNT(*) FROM orders", conn))
            {
                result.TotalOrders = (long)cmd.ExecuteScalar();
            }

            // Doanh thu hôm nay
            using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(total_price),0) FROM orders WHERE created_at >= NOW() - INTERVAL '1 day'", conn))
            {
                result.RevenueToday = (long)(cmd.ExecuteScalar() ?? 0L);
            }

            // Doanh thu 7 ngày gần nhất
            using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(total_price),0) FROM orders WHERE created_at >= NOW() - INTERVAL '7 days'", conn))
            {
                result.Revenue7d = (long)(cmd.ExecuteScalar() ?? 0L);
            }

            // Doanh thu 30 ngày gần nhất
            using (var cmd = new NpgsqlCommand("SELECT COALESCE(SUM(total_price),0) FROM orders WHERE created_at >= NOW() - INTERVAL '30 days'", conn))
            {
                result.Revenue30d = (long)(cmd.ExecuteScalar() ?? 0L);
            }

            // Doanh thu theo ngày (7 ngày)
            result.RevenueByDay = new List<PointDto>();
            using (var cmd = new NpgsqlCommand(@"
                SELECT date_trunc('day', created_at)::date AS d, SUM(total_price)
                FROM orders 
                WHERE created_at >= NOW() - INTERVAL '7 days' 
                GROUP BY d ORDER BY d", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    result.RevenueByDay.Add(new PointDto
                    {
                        Label = r.GetDateTime(0).ToString("yyyy-MM-dd"),
                        Value = r.GetInt64(1)
                    });
                }
            }

            // Doanh thu theo tuần
            result.RevenueByWeek = new List<PointDto>();
            using (var cmd = new NpgsqlCommand(@"
                SELECT date_trunc('week', created_at)::date AS w, SUM(total_price)
                FROM orders 
                WHERE created_at >= NOW() - INTERVAL '8 weeks' 
                GROUP BY w ORDER BY w", conn))
            using (var r = cmd.ExecuteReader())
            {
                while (r.Read())
                {
                    result.RevenueByWeek.Add(new PointDto
                    {
                        Label = r.GetDateTime(0).ToString("yyyy-MM-dd"),
                        Value = r.GetInt64(1)
                    });
                }
            }

            // Top 5 sản phẩm bán chạy
            result.TopProducts = new List<TopProductDto>();
            using (var cmd = new NpgsqlCommand(@"
                SELECT p.name, SUM(oi.quantity) AS qty, SUM(oi.total) AS revenue
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

    // ============================
    //          DTO MODELS
    // ============================

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
        public string Label { get; set; } = "";
        public long Value { get; set; }
    }

    public class TopProductDto
    {
        public string Name { get; set; } = "";
        public long Quantity { get; set; }
        public long Revenue { get; set; }
    }
}
