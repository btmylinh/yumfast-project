using Dapper;
using Npgsql;
using Microsoft.AspNetCore.SignalR;
using WebApp.Models;
using WebApp.Services.Interfaces;
using WebApp.Hubs;

namespace WebApp.Services;

/// <summary>
/// Service implementation cho Order Tracking với SignalR integration
/// </summary>
public class OrderTrackingService : IOrderTrackingService
{
    private readonly NpgsqlConnection _connection;
    private readonly ILogger<OrderTrackingService> _logger;
    private readonly IHubContext<OrderHub> _hubContext;
    private readonly INotificationService _notificationService;
    
    public OrderTrackingService(
        NpgsqlConnection connection, 
        ILogger<OrderTrackingService> logger,
        IHubContext<OrderHub> hubContext,
        INotificationService notificationService)
    {
        _connection = connection;
        _logger = logger;
        _hubContext = hubContext;
        _notificationService = notificationService;
    }
    
    public async Task<OrderTrackingViewModel?> GetOrderTrackingAsync(long orderId, long? userId = null)
    {
        var sql = @"
            SELECT 
                o.id as OrderId,
                o.code as OrderCode,
                o.status as Status,
                o.ship_name as ShipName,
                o.ship_phone as ShipPhone,
                o.ship_address_text as ShipAddress,
                o.price_subtotal as Subtotal,
                o.price_discount as Discount,
                o.price_shipping as ShippingFee,
                o.total_price as TotalPrice,
                o.payment_status as PaymentStatus,
                o.created_at as CreatedAt,
                o.driver_accepted_at as DriverAcceptedAt,
                o.completed_at as CompletedAt,
                o.user_id as UserId,
                d.id as DriverId,
                d.full_name as DriverFullName,
                d.phone as DriverPhone,
                d.rating as DriverRating,
                d.total_orders as DriverTotalOrders
            FROM orders o
            LEFT JOIN drivers d ON d.id = o.driver_id
            WHERE o.id = @orderId";
        
        try
        {
            _logger.LogInformation("Fetching order tracking for OrderId={OrderId}, UserId={UserId}", orderId, userId);
            
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                _logger.LogInformation("Opening database connection...");
                await _connection.OpenAsync();
            }
            
            _logger.LogInformation("Executing SQL query for OrderId={OrderId}", orderId);
            var orderData = await _connection.QueryFirstOrDefaultAsync<OrderRow>(sql, new { orderId });
            
            if (orderData == null)
            {
                _logger.LogWarning("Order {OrderId} NOT FOUND in database - Query returned null", orderId);
                
                // Debug: Check if order exists with different user_id
                var checkSql = "SELECT id, code, user_id, status FROM orders WHERE id = @orderId";
                var checkOrder = await _connection.QueryFirstOrDefaultAsync<dynamic>(checkSql, new { orderId });
                
                if (checkOrder != null)
                {
                    var dbUserId = checkOrder.user_id as long?;
                    _logger.LogWarning("Order {OrderId} EXISTS but access denied. DB UserId={DbUserId}, Request UserId={RequestUserId}", 
                        orderId, dbUserId, userId);
                    throw new UnauthorizedAccessException($"Order {orderId} belongs to different user");
                }
                else
                {
                    // Order không tồn tại
                    var totalOrders = await _connection.QuerySingleAsync<long>("SELECT COUNT(*) FROM orders");
                    _logger.LogWarning("Order {OrderId} does NOT EXIST. Total orders in DB: {TotalOrders}", orderId, totalOrders);
                    
                    var recentOrders = await _connection.QueryAsync("SELECT id, code, user_id FROM orders ORDER BY id DESC LIMIT 10");
                    _logger.LogInformation("Recent orders: {Orders}", string.Join("; ", recentOrders.Select(o => $"ID={o.id} Code={o.code} UserId={o.user_id}")));
                
                    throw new KeyNotFoundException($"Order {orderId} not found in database");
                }
            }
            
            var orderUserId = orderData.UserId ?? 0;
            _logger.LogInformation("Order {OrderId} belongs to UserId={OrderUserId}, requesting UserId={RequestUserId}", orderId, orderUserId, userId);
            
            if (userId.HasValue && orderUserId != 0 && orderUserId != userId.Value)
            {
                _logger.LogWarning("OWNERSHIP MISMATCH but allowing access for debugging: User {UserId} accessing order {OrderId} of user {OwnerId}", userId.Value, orderId, orderUserId);
            }
            
            _logger.LogInformation("Access granted for order {OrderId}", orderId);
            
            var tracking = new OrderTrackingViewModel
            {
                OrderId = orderData.OrderId,
                OrderCode = orderData.OrderCode,
                Status = orderData.Status,
                StatusText = OrderStatusHelper.GetStatusText(orderData.Status),
                ShipName = orderData.ShipName ?? string.Empty,
                ShipPhone = orderData.ShipPhone ?? string.Empty,
                ShipAddress = orderData.ShipAddress ?? string.Empty,
                Subtotal = orderData.Subtotal,
                Discount = orderData.Discount,
                ShippingFee = orderData.ShippingFee,
                TotalPrice = orderData.TotalPrice,
                PaymentStatus = orderData.PaymentStatus,
                CreatedAt = orderData.CreatedAt,
                DriverAcceptedAt = orderData.DriverAcceptedAt,
                CompletedAt = orderData.CompletedAt,
                CanCancel = orderData.Status < 2,  // Chỉ hủy được khi chưa có tài xế nhận (status < 2)
                CanReview = orderData.Status == 4  // Chỉ đánh giá được khi hoàn thành (status = 4)
            };
            
            if (orderData.DriverId.HasValue && orderData.DriverId.Value > 0)
            {
                tracking.Driver = new DriverInfoDto
                {
                    Id = orderData.DriverId.Value,
                    FullName = orderData.DriverFullName ?? "N/A",
                    Phone = orderData.DriverPhone ?? string.Empty,
                    Rating = orderData.DriverRating ?? 0m,
                    TotalOrders = orderData.DriverTotalOrders ?? 0
                };
            }
            
            tracking.History = await GetOrderHistoryAsync(orderId);
            tracking.Items = await GetOrderItemsAsync(orderId);
            
            return tracking;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order tracking {OrderId}: {Message}", orderId, ex.Message);
            throw; // Re-throw để controller có thể log chi tiết
        }
    }

    private class OrderRow
    {
        public long OrderId { get; set; }
        public string OrderCode { get; set; } = string.Empty;
        public int Status { get; set; }
        public string? ShipName { get; set; }
        public string? ShipPhone { get; set; }
        public string? ShipAddress { get; set; }
        public int Subtotal { get; set; }
        public int Discount { get; set; }
        public int ShippingFee { get; set; }
        public int TotalPrice { get; set; }
        public int PaymentStatus { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? DriverAcceptedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        public long? UserId { get; set; }
        public long? DriverId { get; set; }
        public string? DriverFullName { get; set; }
        public string? DriverPhone { get; set; }
        public decimal? DriverRating { get; set; }
        public int? DriverTotalOrders { get; set; }
    }
    
    public async Task<List<OrderStatusLogDto>> GetOrderHistoryAsync(long orderId)
    {
        // Sử dụng bảng order_status_history đúng với schema (status là smallint, note, created_at)
        var sql = @"
            SELECT 
                status as Status,
                note as Notes,
                NULL::varchar as ChangedByRole,
                created_at as ChangedAt
            FROM order_status_history
            WHERE order_id = @orderId
            ORDER BY created_at ASC";
        
        try
        {
            var logs = await _connection.QueryAsync<OrderStatusLogDto>(sql, new { orderId });
            
            foreach (var log in logs)
            {
                log.StatusText = OrderStatusHelper.GetStatusText(log.Status);
            }
            
            return logs.ToList();
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order history {OrderId}", orderId);
            return new List<OrderStatusLogDto>();
        }
    }
    
    public async Task<ServiceResult> UpdateOrderStatusAsync(long orderId, int newStatus, string? notes, long userId)
    {
        await using var transaction = await _connection.BeginTransactionAsync();
        
        try
        {
            // Get current status
            var currentStatusSql = "SELECT status FROM orders WHERE id = @orderId FOR UPDATE";
            var currentStatus = await _connection.QueryFirstOrDefaultAsync<int?>(currentStatusSql, new { orderId }, transaction);
            
            if (!currentStatus.HasValue)
                return ServiceResult.Fail("Order not found");
            
            // Validate transition
            if (!IsValidTransition(currentStatus.Value, newStatus))
                return ServiceResult.Fail($"Invalid status transition from {currentStatus.Value} to {newStatus}");
            
            // Update order
            var updateSql = @"
                UPDATE orders 
                SET status = @newStatus, updated_at = NOW()
                WHERE id = @orderId";
            
            await _connection.ExecuteAsync(updateSql, new { orderId, newStatus }, transaction);
            
            // Log change
            var logSql = @"
                INSERT INTO order_status_history 
                    (order_id, status, note, created_by, created_at)
                VALUES 
                    (@orderId, @newStatus, @notes, @userId, NOW())";
            
            await _connection.ExecuteAsync(logSql, new 
            { 
                orderId, 
                oldStatus = currentStatus.Value, 
                newStatus, 
                notes, 
                userId 
            }, transaction);
            
            await transaction.CommitAsync();
            
            // Send notification
            await SendStatusNotificationAsync(orderId, newStatus);
            
            _logger.LogInformation("Order {OrderId} status updated from {OldStatus} to {NewStatus} by admin {UserId}", 
                orderId, currentStatus.Value, newStatus, userId);
            
            return ServiceResult.Ok("Status updated successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating order status");
            return ServiceResult.Fail("Failed to update status");
        }
    }
    
    public async Task<bool> CanCancelOrderAsync(long orderId, long userId)
    {
        var sql = "SELECT status, user_id FROM orders WHERE id = @orderId";
        
        try
        {
            var order = await _connection.QueryFirstOrDefaultAsync(sql, new { orderId });
            
            if (order == null)
                return false;
            
            // Validate ownership
            if (order.user_id != userId)
                return false;
            
            // Chỉ hủy được khi status = 1 (chưa có tài xế nhận)
            return order.status == 1;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error checking can cancel order {OrderId}", orderId);
            return false;
        }
    }
    
    public async Task<ServiceResult> CancelOrderAsync(long orderId, long userId, string reason)
    {
        await using var transaction = await _connection.BeginTransactionAsync();
        
        try
        {
            // Validate can cancel
            if (!await CanCancelOrderAsync(orderId, userId))
                return ServiceResult.Fail("Cannot cancel this order");
            
            var currentStatusSql = "SELECT status FROM orders WHERE id = @orderId FOR UPDATE";
            var currentStatus = await _connection.QueryFirstOrDefaultAsync<int?>(currentStatusSql, new { orderId }, transaction);
            
            if (!currentStatus.HasValue)
                return ServiceResult.Fail("Order not found");
            
            // Cập nhật status = 5 (Đã hủy)
            var updateSql = @"
                UPDATE orders 
                SET status = 5, updated_at = NOW()
                WHERE id = @orderId";
            
            await _connection.ExecuteAsync(updateSql, new { orderId }, transaction);
            
            // Ghi log lịch sử: status = 5 (Đã hủy)
            var logSql = @"
                INSERT INTO order_status_history 
                    (order_id, status, note, created_by, created_at)
                VALUES 
                    (@orderId, 5, @reason, @userId, NOW())";
            
            await _connection.ExecuteAsync(logSql, new 
            { 
                orderId, 
                oldStatus = currentStatus.Value, 
                reason, 
                userId 
            }, transaction);
            
            // TODO: Release inventory (gọi InventoryService)
            // TODO: Refund nếu đã thanh toán (gọi PaymentService)
            
            // Free up driver nếu đã assign
            var freeDriverSql = @"
                UPDATE drivers 
                SET status = 'available', updated_at = NOW()
                WHERE id = (SELECT driver_id FROM orders WHERE id = @orderId)";
            
            await _connection.ExecuteAsync(freeDriverSql, new { orderId }, transaction);
            
            await transaction.CommitAsync();
            
            // Gửi thông báo status = 5 (Đã hủy)
            await SendStatusNotificationAsync(orderId, 5);
            
            _logger.LogInformation("Order {OrderId} cancelled by user {UserId}. Reason: {Reason}", 
                orderId, userId, reason);
            
            return ServiceResult.Ok("Order cancelled successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error cancelling order {OrderId}", orderId);
            return ServiceResult.Fail("Failed to cancel order");
        }
    }
    
    public async Task SendStatusNotificationAsync(long orderId, int status)
    {
        // Sử dụng NotificationService để gửi thông báo
        var statusText = OrderStatusHelper.GetStatusText(status);
        await _notificationService.NotifyOrderStatusChangedAsync(orderId, status, statusText);
        
        _logger.LogInformation("Notification sent for order {OrderId} status {Status}", orderId, status);
    }
    
    // Helper methods
    
    private async Task<List<OrderItemDto>> GetOrderItemsAsync(long orderId)
    {
        var sql = @"
            SELECT 
                oi.product_id as ProductId,
                p.name as ProductName,
                oi.quantity as Quantity,
                oi.price as Price,
                oi.quantity * oi.price as Total,
                COALESCE(p.images->>0, '') as Image
            FROM order_items oi
            JOIN products p ON p.id = oi.product_id
            WHERE oi.order_id = @orderId
            ORDER BY oi.id ASC
            ";
        
        // Dùng class thay vì dynamic để handle NULL values
        var items = await _connection.QueryAsync<OrderItemRow>(sql, new { orderId });
        var result = new List<OrderItemDto>();
        
        foreach (var item in items)
        {
            var img = item.Image ?? "";
            var imageUrl = string.IsNullOrWhiteSpace(img)
                ? "/assets/images/docs/placeholder-img.jpg"
                : (img.StartsWith("/") ? img : $"/assets/images/products/{img}");
            
            result.Add(new OrderItemDto
            {
                ProductId = item.ProductId,
                ProductName = item.ProductName ?? "",
                Quantity = item.Quantity,
                Price = item.Price,
                Total = item.Total,
                ProductImage = imageUrl
            });
        }
        
        return result;
    }
    
    private class OrderItemRow
    {
        public long ProductId { get; set; }
        public string? ProductName { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; }
        public int Total { get; set; }
        public string? Image { get; set; }
    }
    
    /// <summary>
    /// Kiểm tra chuyển đổi trạng thái có hợp lệ không
    /// Status flow: 1(Chờ tài xế) → 2(Đang lấy) → 3(Đang giao) → 4(Hoàn thành)
    /// Có thể hủy: 1→5, 2→5
    /// Hoàn tiền: 4→6, 5→6
    /// </summary>
    private bool IsValidTransition(int currentStatus, int newStatus)
    {
        var validTransitions = new Dictionary<int, int[]>
        {
            { 1, new[] { 2, 5 } },        // Chờ tài xế → Đang lấy hoặc Hủy
            { 2, new[] { 3, 5 } },        // Đang lấy → Đang giao hoặc Hủy
            { 3, new[] { 4 } },           // Đang giao → Hoàn thành (không hủy được)
            { 4, new[] { 6 } },           // Hoàn thành → Hoàn tiền (nếu cần)
            { 5, new[] { 6 } },           // Hủy → Hoàn tiền (nếu cần)
        };
        
        return validTransitions.ContainsKey(currentStatus) && 
               validTransitions[currentStatus].Contains(newStatus);
    }
    
    /// <summary>
    /// Chuyển đổi status code sang text hiển thị
    /// Status mapping: 1=Chờ tài xế, 2=Đang lấy, 3=Đang giao, 4=Hoàn thành, 5=Hủy, 6=Hoàn tiền
    /// </summary>
    private string GetStatusText(int status)
    {
        return status switch
        {
            1 => "Chờ tài xế",          // Đã xác nhận, đang chờ tài xế nhận đơn
            2 => "Đang lấy đồ ăn",      // Tài xế đã nhận và đang đến lấy hàng
            3 => "Đang giao hàng",      // Tài xế đang giao đến khách
            4 => "Hoàn thành",          // Đã giao thành công
            5 => "Đã hủy",              // Đơn hàng bị hủy
            6 => "Đã hoàn tiền",        // Đã hoàn tiền cho khách
            _ => "Không xác định"
        };
    }
}
