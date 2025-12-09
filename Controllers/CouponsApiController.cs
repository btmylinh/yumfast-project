using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/coupons")]
    public class CouponsApiController : ControllerBase
    {
        private readonly IConfiguration _config;
        public CouponsApiController(IConfiguration config) { _config = config; }

        [HttpGet("active")]
        public IActionResult GetActive([FromQuery] long? productId)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            var where = "WHERE status=1 AND start_at <= NOW() AND end_at > NOW() AND used_count < total";
            if (productId.HasValue) where += " AND (product_id IS NULL OR product_id=@pid)"; // show global or specific
            using var cmd = new NpgsqlCommand($@"SELECT id, code, name, type, value, start_at, end_at, description, total, used_count, product_id
                                               FROM coupons {where}
                                               ORDER BY start_at DESC", conn);
            if (productId.HasValue) cmd.Parameters.AddWithValue("@pid", productId.Value);
            var list = new List<CouponDto>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var total = r.GetInt32(8);
                var used = r.GetInt32(9);
                list.Add(new CouponDto
                {
                    Id = r.GetInt64(0),
                    Code = r.GetString(1),
                    Name = r.GetString(2),
                    Type = r.GetInt16(3),
                    Value = r.GetInt32(4),
                    StartAt = r.GetDateTime(5),
                    EndAt = r.GetDateTime(6),
                    Description = r.IsDBNull(7) ? string.Empty : r.GetString(7),
                    Total = total,
                    UsedCount = used,
                    Remaining = total - used,
                    ProductId = r.IsDBNull(10) ? (long?)null : r.GetInt64(10)
                });
            }
            return Ok(new { data = list });
        }
    }

    public class CouponDto
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public string Name { get; set; } = string.Empty;
        public short Type { get; set; }
        public int Value { get; set; }
        public DateTime StartAt { get; set; }
        public DateTime EndAt { get; set; }
        public string Description { get; set; } = string.Empty;
        public int Total { get; set; }
        public int UsedCount { get; set; }
        public int Remaining { get; set; }
        public long? ProductId { get; set; }
    }
}
