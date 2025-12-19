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
        [Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = "Cookies," + Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
        public IActionResult Get(
            [FromQuery] string? search,
            [FromQuery] int? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = "created_at",
            [FromQuery] string? sortDirection = "desc",
            [FromQuery] string? paymentMethod = null,
            [FromQuery] string? fromDate = null,
            [FromQuery] string? toDate = null,
            [FromQuery] int? minPrice = null,
            [FromQuery] int? maxPrice = null)
        {
            DateTime? fromDateParsed = null;
            DateTime? toDateParsed = null;

            if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var fd))
                fromDateParsed = fd;

            if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var td))
                toDateParsed = td;

            return Ok(_orders.GetOrders(search, status, page, pageSize, sortBy, sortDirection,
                paymentMethod, fromDateParsed, toDateParsed, minPrice, maxPrice));
        }

        /// <summary>
        /// GET /api/orders/statistics - Lấy thống kê đơn hàng
        /// </summary>
        [HttpGet("statistics")]
        [Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = "Cookies," + Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
        public IActionResult GetStatistics()
        {
            return Ok(_orders.GetStatistics());
        }

        /// <summary>
        /// GET /api/orders/export - Xuất danh sách đơn hàng ra file Excel/CSV
        /// </summary>
        [HttpGet("export")]
        [Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = "Cookies," + Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, Roles = "admin")]
        public IActionResult Export(
            [FromQuery] string? search,
            [FromQuery] int? status,
            [FromQuery] string? paymentMethod = null,
            [FromQuery] string? fromDate = null,
            [FromQuery] string? toDate = null,
            [FromQuery] int? minPrice = null,
            [FromQuery] int? maxPrice = null,
            [FromQuery] string format = "excel")
        {
            DateTime? fromDateParsed = null;
            DateTime? toDateParsed = null;

            if (!string.IsNullOrWhiteSpace(fromDate) && DateTime.TryParse(fromDate, out var fd))
                fromDateParsed = fd;

            if (!string.IsNullOrWhiteSpace(toDate) && DateTime.TryParse(toDate, out var td))
                toDateParsed = td;

            var fileBytes = _orders.ExportOrders(search, status, paymentMethod, fromDateParsed, toDateParsed, minPrice, maxPrice, format);

            // For now, both formats return CSV (Excel can be enabled after EPPlus setup)
            var fileName = $"orders_export_{DateTime.Now:yyyyMMdd_HHmmss}.csv";
            var contentType = "text/csv";

            return File(fileBytes, contentType, fileName);
        }

        /// <summary>
        /// GET /api/orders/active - Lấy đơn hàng đang active (status != 4 và != 5)
        /// </summary>
        [HttpGet("active")]
        [Microsoft.AspNetCore.Authorization.Authorize]
        public IActionResult GetActiveOrder()
        {
            var userIdStr = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!long.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            // Lấy đơn hàng active (status 1, 2, 3 - không phải 4=Hoàn thành, 5=Đã hủy, 6=Đã hoàn tiền)
            using var cmd = new NpgsqlCommand(@"
                SELECT o.id, o.code, o.status, o.created_at,
                       o.ship_name, o.ship_phone, o.ship_address_text,
                       o.total_price, o.driver_id,
                       d.full_name as driver_name, d.phone as driver_phone
                FROM orders o
                LEFT JOIN drivers d ON d.id = o.driver_id
                WHERE o.user_id = @uid 
                  AND o.status IN (1, 2, 3)
                ORDER BY o.created_at DESC
                LIMIT 1", conn);

            cmd.Parameters.AddWithValue("@uid", userId);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
            {
                return Ok(new { success = true, data = (object?)null });
            }

            var order = new
            {
                orderId = reader.GetInt64(0),
                orderCode = reader.GetString(1),
                status = reader.GetInt32(2),
                createdAt = reader.GetDateTime(3),
                shipName = reader.IsDBNull(4) ? "" : reader.GetString(4),
                shipPhone = reader.IsDBNull(5) ? "" : reader.GetString(5),
                shipAddress = reader.IsDBNull(6) ? "" : reader.GetString(6),
                totalPrice = reader.GetInt32(7),
                driverId = reader.IsDBNull(8) ? (long?)null : reader.GetInt64(8),
                driverName = reader.IsDBNull(9) ? "" : reader.GetString(9),
                driverPhone = reader.IsDBNull(10) ? "" : reader.GetString(10)
            };

            return Ok(new { success = true, data = order });
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
            // ✅ LẤY USER ID CHUẨN
            var userIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (string.IsNullOrWhiteSpace(userIdStr) || !long.TryParse(userIdStr, out var userId))
            {
                return Unauthorized(new { message = "invalid_user" });
            }

            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var where = "WHERE o.user_id = @uid";
            var statusList = new List<int>();

            // Parse status: "1,2,3"
            if (!string.IsNullOrWhiteSpace(status))
            {
                statusList = status.Split(',')
                    .Select(s => int.TryParse(s.Trim(), out var n) ? n : -1)
                    .Where(n => n >= 0)
                    .ToList();

                if (statusList.Any())
                    where += " AND o.status = ANY(@statusArray)";
            }

            // Count
            using var countCmd = new NpgsqlCommand($"SELECT COUNT(*) FROM orders o {where}", conn);
            countCmd.Parameters.AddWithValue("@uid", userId);
            if (statusList.Any())
                countCmd.Parameters.AddWithValue("@statusArray", statusList.ToArray());

            var totalCount = (long)(countCmd.ExecuteScalar() ?? 0L);

            // Orders - Include review status
            using var cmd = new NpgsqlCommand($@"
        SELECT o.id, o.code, o.ship_name, o.ship_phone, o.ship_address_text,
               o.price_subtotal, o.price_discount, o.price_shipping, o.total_price,
               o.payment_status, o.status, o.created_at,
               EXISTS(SELECT 1 FROM order_reviews WHERE order_id = o.id) as has_review
        FROM orders o
        {where}
        ORDER BY o.created_at DESC
        LIMIT @limit OFFSET @offset", conn);

            cmd.Parameters.AddWithValue("@uid", userId);
            if (statusList.Any())
                cmd.Parameters.AddWithValue("@statusArray", statusList.ToArray());
            cmd.Parameters.AddWithValue("@limit", pageSize);
            cmd.Parameters.AddWithValue("@offset", (page - 1) * pageSize);

            var orderData = new List<(long id, string code, string name, string phone, string address,
                int subtotal, int discount, int shipping, int total, int payment, int status, DateTime created, bool hasReview)>();

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
                        reader.GetDateTime(11),
                        reader.GetBoolean(12) // has_review
                    ));
                }
            }

            var orders = new List<object>();

            foreach (var o in orderData)
            {
                var items = new List<object>();
                using var ic = new NpgsqlCommand(@"
            SELECT oi.product_id, p.name, oi.quantity, oi.price,
                   COALESCE(p.images->>0,'')
            FROM order_items oi
            JOIN products p ON p.id = oi.product_id
            WHERE oi.order_id = @oid
            ORDER BY oi.id", conn);

                ic.Parameters.AddWithValue("@oid", o.id);

                using var ir = ic.ExecuteReader();
                while (ir.Read())
                {
                    var img = ir.GetString(4);
                    items.Add(new
                    {
                        productId = ir.GetInt64(0),
                        productName = ir.GetString(1),
                        quantity = ir.GetInt32(2),
                        price = ir.GetInt32(3),
                        imageUrl = string.IsNullOrWhiteSpace(img)
                            ? "/assets/images/docs/placeholder-img.jpg"
                            : (img.StartsWith("/") ? img : $"/assets/images/products/{img}")
                    });
                }

                orders.Add(new
                {
                    orderId = o.id,
                    orderCode = o.code,
                    shipName = o.name,
                    shipPhone = o.phone,
                    shipAddress = o.address,
                    subtotal = o.subtotal,
                    discount = o.discount,
                    shippingFee = o.shipping,
                    totalPrice = o.total,
                    paymentStatus = o.payment,
                    status = o.status,
                    createdAt = o.created,
                    items,
                    canCancel = o.status == 1,
                    canReview = o.status == 4 && !o.hasReview, // Chỉ có thể đánh giá nếu chưa đánh giá
                    hasReview = o.hasReview // Đánh dấu đơn đã được đánh giá
                });
            }

            return Ok(new
            {
                success = true,
                data = orders,
                totalCount,
                page,
                pageSize,
                totalPages = (int)Math.Ceiling(totalCount / (double)pageSize)
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
                id = r.GetInt64(0),
                code = r.GetString(1),
                userId = orderUserId,
                shipName = r.IsDBNull(3) ? "" : r.GetString(3),
                shipPhone = r.IsDBNull(4) ? "" : r.GetString(4),
                shipAddress = r.IsDBNull(5) ? "" : r.GetString(5),
                subtotal = r.GetInt32(6),
                discount = r.GetInt32(7),
                shippingFee = r.GetInt32(8),
                total = r.GetInt32(9),
                paymentStatus = r.GetInt16(10),
                status = r.GetInt16(11),
                createdAt = r.GetDateTime(12),
                updatedAt = r.GetDateTime(13),
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
        [Microsoft.AspNetCore.Authorization.Authorize(AuthenticationSchemes = "Cookies," + Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
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
