using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Services;
using Npgsql;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/checkout")]
    public class CheckoutController : ControllerBase
    {
        private readonly ICheckoutService _checkoutService;
        private readonly IOrdersService _ordersService;
        private readonly IShippingFeeService _shippingService;
        private readonly IInventoryService _inventoryService;
        private readonly IConfiguration _config;
        private readonly ILogger<CheckoutController> _logger;

        public CheckoutController(
            ICheckoutService checkoutService,
            IOrdersService ordersService,
            IShippingFeeService shippingService,
            IInventoryService inventoryService,
            IConfiguration config,
            ILogger<CheckoutController> logger)
        {
            _checkoutService = checkoutService;
            _ordersService = ordersService;
            _shippingService = shippingService;
            _inventoryService = inventoryService;
            _config = config;
            _logger = logger;
        }

        /// <summary>
        /// Create order - Simplified endpoint for frontend
        /// </summary>
        [HttpPost("create-order")]
        [Authorize(AuthenticationSchemes = "Cookies,Bearer")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest req)
        {
            if (req == null || req.Items == null || !req.Items.Any())
            {
                return BadRequest(new { success = false, message = "Giỏ hàng trống" });
            }

            if (req.ShippingAddress == null)
            {
                return BadRequest(new { success = false, message = "Thiếu địa chỉ giao hàng" });
            }

            var userId = GetUserId();
            _logger.LogInformation("CreateOrder - UserId from claims: {UserId}", userId);
            _logger.LogInformation("User claims: {Claims}", string.Join(", ", User.Claims.Select(c => $"{c.Type}={c.Value}")));
            
            if (userId == null)
            {
                _logger.LogWarning("CreateOrder failed - Cannot determine userId");
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập" });
            }

            try
            {
                // Calculate subtotal from DB
                var subtotal = 0;
                await using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
                await conn.OpenAsync();

                foreach (var item in req.Items)
                {
                    await using var cmd = new NpgsqlCommand("SELECT price FROM products WHERE id = @id", conn);
                    cmd.Parameters.AddWithValue("@id", item.ProductId);
                    var priceObj = await cmd.ExecuteScalarAsync();
                    if (priceObj != null && priceObj != DBNull.Value)
                    {
                        subtotal += Convert.ToInt32(priceObj) * item.Quantity;
                    }
                }

                // For now, use fixed shipping fee (can be enhanced later)
                var shippingFee = 30000;
                var discount = 0;
                if (!string.IsNullOrWhiteSpace(req.CouponCode))
                {
                    var calc = await ValidateCouponAsync(req.CouponCode.Trim(), subtotal);
                    if (calc < 0)
                    {
                        return BadRequest(new { success = false, message = "Mã giảm giá không hợp lệ" });
                    }
                    discount = calc;
                }
                var total = subtotal + shippingFee - discount;

                // Create order
                var orderId = await CreateOrderAsync(
                    userId.Value,
                    req.Items.Select(i => new CheckoutItemDto { ProductId = i.ProductId, Quantity = i.Quantity }).ToList(),
                    subtotal,
                    shippingFee,
                    discount,
                    total,
                    req.ShippingAddress.Name ?? "",
                    req.ShippingAddress.Phone ?? "",
                    req.ShippingAddress.Address ?? "",
                    req.PaymentMethod ?? "COD",
                    req.CouponCode
                );

                if (orderId <= 0)
                {
                    return StatusCode(500, new { success = false, message = "Không thể tạo đơn hàng" });
                }

                // Clear cart after successful order (DB + cookie)
                await using (var clearCmd = new NpgsqlCommand("DELETE FROM carts WHERE user_id = @uid", conn))
                {
                    clearCmd.Parameters.AddWithValue("@uid", userId.Value);
                    await clearCmd.ExecuteNonQueryAsync();
                }

                // Reset cart cookie so frontend/offcanvas reflects empty cart
                Response.Cookies.Append("cart", "[]", new CookieOptions
                {
                    HttpOnly = false,
                    IsEssential = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });

                return Ok(new 
                { 
                    success = true, 
                    message = "Đặt hàng thành công!",
                    data = new { orderId, subtotal, shippingFee, total }
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, new { success = false, message = ex.Message });
            }
        }

        /// <summary>
        /// Quote shipping fee trước khi checkout
        /// </summary>
        [HttpPost("quote-shipping")]
        public async Task<IActionResult> QuoteShipping([FromBody] QuoteShippingRequest req)
        {
            if (req == null || req.ZoneId <= 0 || req.Subtotal < 0)
            {
                return BadRequest(new { code = "invalid_request" });
            }

            var (zone, fee) = await _shippingService.QuoteByZoneAsync(req.ZoneId, req.Subtotal);
            
            if (zone == null)
            {
                return BadRequest(new { code = "invalid_zone", message = "Vùng giao hàng không hợp lệ" });
            }

            return Ok(new 
            { 
                zoneId = zone.Id,
                zoneName = zone.Name,
                shippingFee = fee,
                isFreeShipping = fee == 0,
                freeMinimum = zone.FreeMinimum
            });
        }

        /// <summary>
        /// Process checkout với full validation
        /// </summary>
        [HttpPost("process")]
        [Authorize] // Yêu cầu đăng nhập
        public async Task<IActionResult> ProcessCheckout([FromBody] ProcessCheckoutRequest req)
        {
            if (req == null || req.Items == null || !req.Items.Any())
            {
                return BadRequest(new { code = "empty_cart" });
            }

            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized();
            }

            // 1️⃣ Recalculate subtotal từ DB (KHÔNG TIN CLIENT)
            var subtotal = await CalculateSubtotalFromDbAsync(req.Items);

            // 2️⃣ Validate shipping fee từ DB
            var (zone, shippingFee) = await _shippingService.QuoteByZoneAsync(req.ZoneId, subtotal);
            if (zone == null)
            {
                return BadRequest(new { code = "invalid_zone", message = "Vùng giao hàng không hợp lệ" });
            }

            // 3️⃣ Validate coupon (nếu có)
            var discount = 0;
            if (!string.IsNullOrEmpty(req.CouponCode))
            {
                discount = await ValidateCouponAsync(req.CouponCode, subtotal);
                if (discount < 0)
                {
                    return BadRequest(new { code = "invalid_coupon", message = "Mã giảm giá không hợp lệ" });
                }
            }

            // 4️⃣ Tính tổng cuối cùng
            var total = subtotal + shippingFee - discount;
            if (total < 0) total = 0;

            // 5️⃣ Validate và confirm inventory
            foreach (var item in req.Items)
            {
                var hasStock = await _inventoryService.CheckStockAsync(item.ProductId, item.Quantity);
                if (!hasStock)
                {
                    return BadRequest(new 
                    { 
                        code = "stock_insufficient", 
                        message = $"Sản phẩm ID {item.ProductId} không đủ số lượng" 
                    });
                }
            }

            // 6️⃣ Confirm inventory (reserve -> confirmed)
            foreach (var item in req.Items)
            {
                var confirmed = await _inventoryService.ConfirmStockAsync(item.ProductId, item.Quantity);
                if (!confirmed)
                {
                    // TODO: Rollback previous confirmations
                    return StatusCode(500, new { code = "stock_confirm_failed" });
                }
            }

            // 7️⃣ Create order
            var orderId = await CreateOrderAsync(
                userId.Value,
                req.Items,
                subtotal,
                shippingFee,
                discount,
                total,
                req.ShipName,
                req.ShipPhone,
                req.ShipAddress,
                req.PaymentMethod,
                req.CouponCode
            );

            if (orderId <= 0)
            {
                return StatusCode(500, new { code = "order_creation_failed" });
            }

            return Ok(new 
            { 
                orderId,
                subtotal,
                shippingFee,
                discount,
                total,
                message = "Đặt hàng thành công!"
            });
        }

        #region Helper Methods

        private long? GetUserId()
        {
            var userIdStr = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (long.TryParse(userIdStr, out var userId))
            {
                return userId;
            }
            return null;
        }

        private async Task<int> CalculateSubtotalFromDbAsync(List<CheckoutItemDto> items)
        {
            await using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            var subtotal = 0;
            foreach (var item in items)
            {
                await using var cmd = new NpgsqlCommand("SELECT price FROM products WHERE id = @id", conn);
                cmd.Parameters.AddWithValue("@id", item.ProductId);

                var priceObj = await cmd.ExecuteScalarAsync();
                if (priceObj != null && priceObj != DBNull.Value)
                {
                    var price = Convert.ToInt32(priceObj);
                    subtotal += price * item.Quantity;
                }
            }

            return subtotal;
        }

        private async Task<int> ValidateCouponAsync(string couponCode, int subtotal)
        {
            await using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                SELECT type, value, total, used_count, min_order, max_discount
                FROM coupons
                WHERE code = @code 
                  AND status = 1 
                  AND start_at <= NOW() 
                  AND end_at > NOW()
                  AND used_count < total", conn);

            cmd.Parameters.AddWithValue("@code", couponCode);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return -1; // Invalid coupon
            }

            var type = reader.GetInt16(0); // 1=percentage, 2=fixed
            var value = reader.GetInt32(1);
            var minOrder = reader.IsDBNull(4) ? 0 : reader.GetInt32(4);
            var maxDiscount = reader.IsDBNull(5) ? 0 : reader.GetInt32(5);

            if (minOrder > 0 && subtotal < minOrder)
                return -1;

            int discount;
            if (type == 1) // Percentage
            {
                discount = (int)Math.Floor(subtotal * (value / 100.0));
                if (maxDiscount > 0 && discount > maxDiscount) discount = maxDiscount;
            }
            else // Fixed amount
            {
                discount = Math.Min(value, subtotal);
            }
            return discount;
        }

        private async Task<long> CreateOrderAsync(
            long userId,
            List<CheckoutItemDto> items,
            int subtotal,
            int shippingFee,
            int discount,
            int total,
            string shipName,
            string shipPhone,
            string shipAddress,
            string paymentMethod,
            string? couponCode)
        {
            _logger.LogInformation("CreateOrderAsync - UserId parameter value: {UserId}, Type: {Type}", userId, userId.GetType().Name);
            
            await using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            // Generate order code
            var orderCode = $"ORD{DateTime.Now:yyyyMMddHHmmss}";

            // Find coupon id by code (if any)
            long? couponId = null;
            if (!string.IsNullOrEmpty(couponCode))
            {
                await using (var findCoupon = new NpgsqlCommand("SELECT id FROM coupons WHERE code=@code", conn))
                {
                    findCoupon.Parameters.AddWithValue("@code", couponCode);
                    var cid = await findCoupon.ExecuteScalarAsync();
                    if (cid != null && cid != DBNull.Value)
                        couponId = Convert.ToInt64(cid);
                }
            }

            // Tạo đơn hàng với status = 1 (Đã xác nhận và chờ tài xế)
            await using var cmd = new NpgsqlCommand(@"
                INSERT INTO orders (
                    code, user_id, coupons_id, ship_name, ship_phone, ship_address_text,
                    price_subtotal, price_discount, price_shipping, total_price,
                    payment_status, status, created_at, updated_at
                )
                VALUES (
                    @code, @uid, @couponId, @name, @phone, @addr,
                    @subtotal, @discount, @shipping, @total,
                    0, 1, NOW(), NOW()
                )
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("@code", orderCode);
            cmd.Parameters.AddWithValue("@uid", userId);
            cmd.Parameters.AddWithValue("@name", shipName);
            cmd.Parameters.AddWithValue("@phone", shipPhone);
            cmd.Parameters.AddWithValue("@addr", shipAddress);
            cmd.Parameters.AddWithValue("@subtotal", subtotal);
            cmd.Parameters.AddWithValue("@discount", discount);
            cmd.Parameters.AddWithValue("@shipping", shippingFee);
            cmd.Parameters.AddWithValue("@total", total);
            cmd.Parameters.AddWithValue("@couponId", (object?)couponId ?? DBNull.Value);

            _logger.LogInformation("Executing INSERT with @uid={UserId}, @code={OrderCode}, @total={Total}", userId, orderCode, total);
            var orderIdObj = await cmd.ExecuteScalarAsync();
            var orderId = orderIdObj != null ? Convert.ToInt64(orderIdObj) : 0L;
            
            _logger.LogInformation("Order created with ID={OrderId}", orderId);

            if (orderId > 0)
            {
                // Verify what was actually inserted
                await using (var verifyCmd = new NpgsqlCommand("SELECT user_id FROM orders WHERE id=@orderId", conn))
                {
                    verifyCmd.Parameters.AddWithValue("@orderId", orderId);
                    var dbUserId = await verifyCmd.ExecuteScalarAsync();
                    _logger.LogInformation("Verified: Order {OrderId} has user_id={DbUserId} in database", orderId, dbUserId);
                }
                
                // Ghi log lịch sử: status = 1 (Đã xác nhận và chờ tài xế)
                await using var historyCmd = new NpgsqlCommand(@"
                    INSERT INTO order_status_history (order_id, status, note, created_by, created_at)
                    VALUES (@orderId, 1, 'Đơn hàng đã được tạo và đang tìm kiếm tài xế', @userId, NOW())", conn);
                historyCmd.Parameters.AddWithValue("@orderId", orderId);
                historyCmd.Parameters.AddWithValue("@userId", userId);
                await historyCmd.ExecuteNonQueryAsync();
                
                // Insert order items
                foreach (var item in items)
                {
                    await using var itemCmd = new NpgsqlCommand(@"
                        INSERT INTO order_items (order_id, product_id, quantity, price, total)
                        SELECT @oid, @pid, @qty, price, price * @qty
                        FROM products WHERE id = @pid", conn);

                    itemCmd.Parameters.AddWithValue("@oid", orderId);
                    itemCmd.Parameters.AddWithValue("@pid", item.ProductId);
                    itemCmd.Parameters.AddWithValue("@qty", item.Quantity);

                    await itemCmd.ExecuteNonQueryAsync();
                }

                // Update coupon usage nếu có
                if (!string.IsNullOrEmpty(couponCode))
                {
                    await using var couponCmd = new NpgsqlCommand(@"
                        UPDATE coupons SET used_count = used_count + 1 WHERE code = @code", conn);
                    couponCmd.Parameters.AddWithValue("@code", couponCode);
                    await couponCmd.ExecuteNonQueryAsync();
                }

                // Create payment transaction record (if not COD)
                if (!string.IsNullOrEmpty(paymentMethod) && paymentMethod != "COD")
                {
                    var transactionId = $"{paymentMethod}{orderId}{DateTime.Now:yyyyMMddHHmmss}";
                    await using var paymentCmd = new NpgsqlCommand(@"
                        INSERT INTO payment_transactions 
                        (order_id, payment_method, transaction_id, amount, status)
                        VALUES (@orderId, @method, @txnId, @amount, 'pending')", conn);
                    
                    paymentCmd.Parameters.AddWithValue("@orderId", orderId);
                    paymentCmd.Parameters.AddWithValue("@txnId", transactionId);
                    paymentCmd.Parameters.AddWithValue("@method", paymentMethod);
                    paymentCmd.Parameters.AddWithValue("@amount", total);
                    await paymentCmd.ExecuteNonQueryAsync();
                }
            }

            return orderId;
        }

        #endregion
    }

    #region DTOs

    public class QuoteShippingRequest
    {
        public long ZoneId { get; set; }
        public int Subtotal { get; set; }
    }

    public class ProcessCheckoutRequest
    {
        public List<CheckoutItemDto> Items { get; set; } = new();
        public long ZoneId { get; set; }
        public string ShipName { get; set; } = "";
        public string ShipPhone { get; set; } = "";
        public string ShipAddress { get; set; } = "";
        public string PaymentMethod { get; set; } = "COD";
        public string? CouponCode { get; set; }
    }

    public class CheckoutItemDto
    {
        public long ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class CreateOrderRequest
    {
        public List<OrderItemRequest> Items { get; set; } = new();
        public ShippingAddressDto? ShippingAddress { get; set; }
        public string? PaymentMethod { get; set; }
        public string? Notes { get; set; }
        public string? CouponCode { get; set; }
    }

    public class OrderItemRequest
    {
        public long ProductId { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }
    }

    public class ShippingAddressDto
    {
        public string? Name { get; set; }
        public string? Phone { get; set; }
        public string? Address { get; set; }
    }

    #endregion
}
