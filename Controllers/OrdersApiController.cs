using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersApiController : ControllerBase
    {
        private readonly IConfiguration _config;
        public OrdersApiController(IConfiguration config) { _config = config; }

        [HttpGet]
        public IActionResult Get([FromQuery] string? search, [FromQuery] int? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? sortBy = "created_at", [FromQuery] string? sortDirection = "desc")
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            var where = "WHERE 1=1";
            if (!string.IsNullOrWhiteSpace(search)) where += " AND (o.code ILIKE @s OR o.ship_phone ILIKE @s OR u.email ILIKE @s)";
            if (status.HasValue) where += " AND o.status=@st";

            var sort = (sortBy?.ToLower()) switch
            {
                "code" => "o.code",
                "total_price" => "o.total_price",
                "status" => "o.status",
                _ => "o.created_at"
            };
            var dir = sortDirection?.ToLower() == "asc" ? "ASC" : "DESC";

            using var countCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM orders o LEFT JOIN users u ON u.id=o.user_id {where}", conn);
            if (!string.IsNullOrWhiteSpace(search)) countCmd.Parameters.AddWithValue("@s", $"%{search}%");
            if (status.HasValue) countCmd.Parameters.AddWithValue("@st", status.Value);
            var total = (long)countCmd.ExecuteScalar();

            var offset = (page - 1) * pageSize;
            using var cmd = new NpgsqlCommand($@"SELECT o.id, o.code, o.user_id, COALESCE(u.email,''), o.ship_phone, o.total_price, o.payment_status, o.status, o.created_at
                                                FROM orders o LEFT JOIN users u ON u.id=o.user_id
                                                {where}
                                                ORDER BY {sort} {dir} LIMIT @ps OFFSET @off", conn);
            if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@s", $"%{search}%");
            if (status.HasValue) cmd.Parameters.AddWithValue("@st", status.Value);
            cmd.Parameters.AddWithValue("@ps", pageSize);
            cmd.Parameters.AddWithValue("@off", offset);
            var list = new List<OrderDto>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                list.Add(new OrderDto
                {
                    Id = r.GetInt64(0),
                    Code = r.GetString(1),
                    UserId = r.IsDBNull(2) ? (long?)null : r.GetInt64(2),
                    Email = r.GetString(3),
                    Phone = r.IsDBNull(4) ? string.Empty : r.GetString(4),
                    TotalPrice = r.GetInt32(5),
                    PaymentStatus = r.GetInt16(6),
                    Status = r.GetInt16(7),
                    CreatedAt = r.GetDateTime(8)
                });
            }
            return Ok(new { data = list, total });
        }
    }

    public class OrderDto
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public long? UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int TotalPrice { get; set; }
        public short PaymentStatus { get; set; }
        public short Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
