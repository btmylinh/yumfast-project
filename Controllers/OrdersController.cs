using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.SignalR;
using Npgsql;
using WebApp.Hubs;
using WebApp.Services;
using WebApp.Models;
using System.Security.Claims;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/orders")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrdersService _orders;
        private readonly ICheckoutService _checkout;
        private readonly IPaymentService _payment;
        private readonly IConfiguration _config;
        private readonly IHubContext<OrderHub> _orderHub;

        public OrdersController(
            IOrdersService orders,
            ICheckoutService checkout,
            IPaymentService payment,
            IConfiguration config,
            IHubContext<OrderHub> orderHub)
        {
            _orders = orders;
            _checkout = checkout;
            _payment = payment;
            _config = config;
            _orderHub = orderHub;
        }

        // GET /api/orders - Admin only (tất cả orders)
        [HttpGet]
        [Microsoft.AspNetCore.Authorization.Authorize(Roles = "Admin")]
        public IActionResult Get(
            [FromQuery] string? search,
            [FromQuery] int? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = "created_at",
            [FromQuery] string? sortDirection = "desc")
        {
            return Ok(_orders.GetOrders(search, status, page, pageSize, sortBy, sortDirection));
        }

        /// <summary>
        /// GET /api/orders/my-orders - User xem đơn của chính mình
        /// Query: ?status=1,2,3 (comma-separated) hoặc ?status=1 (single value)
        /// </summary>
        [HttpGet("my-orders")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult GetMyOrders(
            [FromQuery] string? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 10)
        {
            var userIdStr = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var where = "WHERE o.user_id = @uid";
            var statusList = new List<int>();
            
            // Parse status filter (có thể là "1" hoặc "1,2,3")
            if (!string.IsNullOrEmpty(status))
            {
                statusList = status.Split(',')
                    .Select(s => int.TryParse(s.Trim(), out var n) ? n : -1)
                    .Where(n => n >= 0)
                    .ToList();
                
                if (statusList.Any())
                {
                    where += $" AND o.status = ANY(@statusArray)";
                }
            }

            // Count total
            using var countCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM orders o {where}", conn);
            countCmd.Parameters.AddWithValue("@uid", userId);
            if (statusList.Any())
            {
                countCmd.Parameters.AddWithValue("@statusArray", statusList.ToArray());
            }
            var totalCount = (long)(countCmd.ExecuteScalar() ?? 0L);

            // Get orders
            using var cmd = new NpgsqlCommand($@"
                SELECT o.id, o.code, o.ship_name, o.ship_phone, o.ship_address_text,
                       o.price_subtotal, o.price_discount, o.price_shipping, o.total_price,
                       o.payment_status, o.status, o.created_at
                FROM orders o
                {where}
                ORDER BY o.created_at DESC
                LIMIT @limit OFFSET @offset", conn);

            cmd.Parameters.AddWithValue("@uid", userId);
            if (statusList.Any())
            {
                cmd.Parameters.AddWithValue("@statusArray", statusList.ToArray());
            }
            cmd.Parameters.AddWithValue("@limit", pageSize);
            cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

            // Đọc tất cả orders trước (đóng reader)
            var orderData = new List<(long orderId, string orderCode, string shipName, string shipPhone, 
                string shipAddress, int subtotal, int discount, int shippingFee, int totalPrice, 
                int paymentStatus, int status, DateTime createdAt)>();
            
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    orderData.Add((
                        reader.GetInt64(0),
                        reader.GetString(1),
                        reader.IsDBNull(2) ? "" : reader.GetString(2),
                        reader.IsDBNull(3) ? "" : reader.GetString(3),
                        reader.IsDBNull(4) ? "" : reader.GetString(4),
                        reader.GetInt32(5),
                        reader.GetInt32(6),
                        reader.GetInt32(7),
                        reader.GetInt32(8),
                        reader.GetInt32(9),
                        reader.GetInt32(10),
                        reader.GetDateTime(11)
                    ));
                }
            }
            
            // Bây giờ mới load items cho từng order (reader đã đóng)
            var orders = new List<object>();
            foreach (var order in orderData)
            {
                // Load items cho order này
                var items = new List<object>();
                using var itemsCmd = new NpgsqlCommand(@"
                    SELECT oi.product_id, p.name, oi.quantity, oi.price, 
                           COALESCE(p.images->>0, '') as image_url
                    FROM order_items oi
                    JOIN products p ON p.id = oi.product_id
                    WHERE oi.order_id = @orderId
                    ORDER BY oi.id ASC", conn);
                itemsCmd.Parameters.AddWithValue("@orderId", order.orderId);
                
                using var itemsReader = itemsCmd.ExecuteReader();
                while (itemsReader.Read())
                {
                    var img = itemsReader.IsDBNull(4) ? "" : itemsReader.GetString(4);
                    // Format image URL: nếu rỗng dùng placeholder, nếu có / thì giữ nguyên, không thì thêm prefix
                    var imageUrl = string.IsNullOrWhiteSpace(img)
                        ? "/assets/images/docs/placeholder-img.jpg"
                        : (img.StartsWith("/") ? img : $"/assets/images/products/{img}");
                    
                    items.Add(new
                    {
                        productId = itemsReader.GetInt64(0),
                        productName = itemsReader.GetString(1),
                        quantity = itemsReader.GetInt32(2),
                        price = itemsReader.GetInt32(3),
                        imageUrl = imageUrl
                    });
                }
                
                orders.Add(new
                {
                    orderId = order.orderId,
                    orderCode = order.orderCode,
                    shipName = order.shipName,
                    shipPhone = order.shipPhone,
                    shipAddress = order.shipAddress,
                    subtotal = order.subtotal,
                    discount = order.discount,
                    shippingFee = order.shippingFee,
                    totalPrice = order.totalPrice,
                    paymentStatus = order.paymentStatus,
                    status = order.status,
                    createdAt = order.createdAt,
                    items = items,
                    canCancel = order.status < 2 // Có thể hủy nếu status < 2 (trước khi tài xế nhận)
                });
            }

            return Ok(new
            {
                success = true,
                data = orders,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
            });
        }

        // GET /api/orders/{id}
        [Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme)]
        [HttpGet("{id:long}")]
        public IActionResult GetById(long id)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT o.id, o.code, o.user_id, o.ship_name, o.ship_phone, o.ship_address_text,
                       o.price_subtotal, o.price_discount, o.price_shipping, o.total_price,
                       o.payment_status, o.status, o.created_at, o.updated_at,
                       COALESCE(u.email,'')
                FROM orders o LEFT JOIN users u ON u.id=o.user_id
                WHERE o.id=@id
            ", conn);
            cmd.Parameters.AddWithValue("@id", id);

            using var r = cmd.ExecuteReader();
            if (!r.Read()) return NotFound(new { message = "not_found" });

            var orderUserId = r.IsDBNull(2) ? (long?)null : r.GetInt64(2);
            var currentUserIdStr = User.FindFirstValue("uid");
            var currentUserRole = User.FindFirstValue(ClaimTypes.Role);

            if (currentUserRole?.ToLowerInvariant() != "admin" && (!long.TryParse(currentUserIdStr, out var currentUserId) || orderUserId != currentUserId))
            {
                return Forbid();
            }

            var dto = new
            {
                id = r.GetInt64(0), code = r.GetString(1), userId = orderUserId,
                shipName = r.IsDBNull(3) ? "" : r.GetString(3), shipPhone = r.IsDBNull(4) ? "" : r.GetString(4), shipAddress = r.IsDBNull(5) ? "" : r.GetString(5),
                subtotal = r.GetInt32(6), discount = r.GetInt32(7), shippingFee = r.GetInt32(8), total = r.GetInt32(9),
                paymentStatus = r.GetInt16(10), status = r.GetInt16(11), createdAt = r.GetDateTime(12), updatedAt = r.GetDateTime(13),
                email = r.GetString(14)
            };
            r.Close();

            // Items
            var items = new List<object>();
            using (var ci = new NpgsqlCommand(@"
                SELECT i.product_id, p.name, i.quantity, i.price, i.total
                FROM order_items i JOIN products p ON p.id=i.product_id
                WHERE i.order_id=@oid
            ", conn))
            {
                ci.Parameters.AddWithValue("@oid", id);
                using var ri = ci.ExecuteReader();
                while (ri.Read())
                {
                    items.Add(new { productId = ri.GetInt64(0), name = ri.GetString(1), quantity = ri.GetInt32(2), price = ri.GetInt32(3), total = ri.GetInt32(4) });
                }
            }

            // History
            var history = new List<object>();
            using (var ch = new NpgsqlCommand(@"SELECT status, note, created_by, created_at FROM order_status_history WHERE order_id=@oid ORDER BY created_at ASC", conn))
            {
                ch.Parameters.AddWithValue("@oid", id);
                using var rh = ch.ExecuteReader();
                while (rh.Read())
                {
                    history.Add(new { status = rh.GetInt16(0), note = rh.IsDBNull(1) ? "" : rh.GetString(1), createdBy = rh.IsDBNull(2) ? 0 : rh.GetInt64(2), createdAt = rh.GetDateTime(3) });
                }
            }

            return Ok(new { data = new { order = dto, items, history } });
        }

        // POST /api/orders/checkout
        [HttpPost("checkout")]
        public async Task<IActionResult> Checkout([FromBody] CheckoutRequest req)
        {
            var cookie = Request.Cookies["cart"] ?? "[]";
            var result = await _checkout.CheckoutAsync(req, cookie, Response);
            return Ok(result);
        }

        // PATCH /api/orders/{id}/status
        public class UpdateStatusRequest { public short Status { get; set; } public string? Note { get; set; } }

        [HttpPatch("{id:long}/status")]
        public async Task<IActionResult> UpdateStatus(long id, [FromBody] UpdateStatusRequest req)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            short oldStatus;
            using (var get = new NpgsqlCommand("SELECT status FROM orders WHERE id=@id", conn))
            {
                get.Parameters.AddWithValue("@id", id);
                var o = await get.ExecuteScalarAsync();
                if (o == null) return NotFound(new { message = "not_found" });
                oldStatus = (short)o;
            }

            using (var tx = conn.BeginTransaction())
            {
                using var upd = new NpgsqlCommand("UPDATE orders SET status=@st, updated_at=NOW() WHERE id=@id", conn, tx);
                upd.Parameters.AddWithValue("@st", req.Status);
                upd.Parameters.AddWithValue("@id", id);
                await upd.ExecuteNonQueryAsync();

                using var ins = new NpgsqlCommand("INSERT INTO order_status_history(order_id, status, note, created_by) VALUES(@oid,@st,@note,@uid)", conn, tx);
                ins.Parameters.AddWithValue("@oid", id);
                ins.Parameters.AddWithValue("@st", req.Status);
                ins.Parameters.AddWithValue("@note", (object?)req.Note ?? (object)DBNull.Value);
                ins.Parameters.AddWithValue("@uid", 0);
                await ins.ExecuteNonQueryAsync();

                await tx.CommitAsync();
            }

            // Broadcast via SignalR
            await _orderHub.Clients.Group($"order-{id}").SendAsync("OrderStatusChanged", id, req.Status, DateTime.UtcNow);
            if (req.Status == 2) await _orderHub.Clients.Group($"order-{id}").SendAsync("OrderShipped", id, "");
            if (req.Status == 3) await _orderHub.Clients.Group($"order-{id}").SendAsync("OrderDelivered", id, DateTime.UtcNow);
            if (req.Status == 4) await _orderHub.Clients.Group($"order-{id}").SendAsync("OrderCancelled", id, req.Note ?? "", DateTime.UtcNow);
            // Also notify all admin dashboards
            await _orderHub.Clients.All.SendAsync("OrderUpdated", id, req.Status);

            return Ok(new { id, status = req.Status });
        }

        // POST /api/orders/{id}/payment
        [HttpPost("{id:long}/payment")]
        public async Task<IActionResult> CreatePayment(long id, [FromBody] PaymentRequest req)
        {
            return Ok(await _payment.CreatePaymentAsync(id, req.Method));
        }

        // CALL BACK
        [HttpGet("payment/callback/vnpay")]
        public async Task<IActionResult> VNPayCallback(long orderId, int success = 1)
        {
            return Ok(await _payment.HandleCallbackAsync(orderId, success == 1, "VNPAY"));
        }

        [HttpGet("payment/callback/momo")]
        public async Task<IActionResult> MoMoCallback(long orderId, int success = 1)
        {
            return Ok(await _payment.HandleCallbackAsync(orderId, success == 1, "MOMO"));
        }
    }
}
