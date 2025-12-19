using Dapper;
using Npgsql;
using Microsoft.AspNetCore.SignalR;
using WebApp.Models;
using WebApp.Services.Interfaces;
using WebApp.Hubs;

namespace WebApp.Services;

/// <summary>
/// Service implementation cho Driver operations với SignalR integration
/// </summary>
public class DriverService : IDriverService
{
    private readonly NpgsqlConnection _connection;
    private readonly ILogger<DriverService> _logger;
    private readonly IHubContext<OrderHub> _hubContext;
    
    public DriverService(
        NpgsqlConnection connection, 
        ILogger<DriverService> logger,
        IHubContext<OrderHub> hubContext)
    {
        _connection = connection;
        _logger = logger;
        _hubContext = hubContext;
    }
    
    /// <summary>
    /// Lấy danh sách đơn hàng chưa có tài xế (status = 1: Chờ tài xế)
    /// </summary>
    public async Task<List<DriverOrderViewModel>> GetAvailableOrdersAsync()
    {
        if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync();
        }
        
        var sql = @"
            SELECT 
                o.id as OrderId,
                o.code as OrderCode,
                o.status as Status,
                o.total_price as TotalPrice,
                o.ship_name as ShipName,
                o.ship_phone as ShipPhone,
                o.ship_address_text as ShipAddress,
                o.created_at as CreatedAt
            FROM orders o
            WHERE o.status = 1 AND o.driver_id IS NULL
            ORDER BY o.id DESC";
        
        try
        {
            var orders = await _connection.QueryAsync<DriverOrderViewModel>(sql);
            var orderList = orders.ToList();
            
            // Load items cho mỗi order
            foreach (var order in orderList)
            {
                order.StatusText = GetStatusText(order.Status);
                
                var itemsSql = @"
                    SELECT 
                        oi.product_id as ProductId,
                        p.name as ProductName,
                        oi.quantity as Quantity,
                        oi.price as Price,
                        oi.quantity * oi.price as Total
                    FROM order_items oi
                    JOIN products p ON p.id = oi.product_id
                    WHERE oi.order_id = @orderId
                    ORDER BY oi.id ASC";
                
                var items = await _connection.QueryAsync<OrderItemDto>(itemsSql, new { orderId = order.OrderId });
                order.Items = items.ToList();
            }
            
            return orderList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting available orders");
            return new List<DriverOrderViewModel>();
        }
    }
    
    /// <summary>
    /// Lấy danh sách đơn hàng của tài xế (status 2,3: Đang lấy đồ ăn, Đang giao hàng)
    /// </summary>
    public async Task<List<DriverOrderViewModel>> GetMyOrdersAsync(long driverId)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync();
        }
        
        var sql = @"
            SELECT 
                o.id as OrderId,
                o.code as OrderCode,
                o.status as Status,
                o.total_price as TotalPrice,
                o.ship_name as ShipName,
                o.ship_phone as ShipPhone,
                o.ship_address_text as ShipAddress,
                o.created_at as CreatedAt,
                o.driver_accepted_at as DriverAcceptedAt,
                o.completed_at as CompletedAt
            FROM orders o
            WHERE o.driver_id = @driverId AND o.status IN (2, 3)
            ORDER BY o.created_at DESC";
        
        try
        {
            var orders = await _connection.QueryAsync<DriverOrderViewModel>(sql, new { driverId });
            var orderList = orders.ToList();
            
            // Load items cho mỗi order
            foreach (var order in orderList)
            {
                order.StatusText = GetStatusText(order.Status);
                
                var itemsSql = @"
                    SELECT 
                        oi.product_id as ProductId,
                        p.name as ProductName,
                        oi.quantity as Quantity,
                        oi.price as Price,
                        oi.quantity * oi.price as Total
                    FROM order_items oi
                    JOIN products p ON p.id = oi.product_id
                    WHERE oi.order_id = @orderId
                    ORDER BY oi.id ASC";
                
                var items = await _connection.QueryAsync<OrderItemDto>(itemsSql, new { orderId = order.OrderId });
                order.Items = items.ToList();
            }
            
            return orderList;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting driver orders for driver {DriverId}", driverId);
            return new List<DriverOrderViewModel>();
        }
    }
    
    public async Task<DriverOrderViewModel?> GetOrderDetailAsync(long orderId, long driverId)
    {
        var sql = @"
            SELECT 
                o.id as OrderId,
                o.code as OrderCode,
                o.status as Status,
                o.total_price as TotalPrice,
                o.ship_name as ShipName,
                o.ship_phone as ShipPhone,
                o.ship_address_text as ShipAddress,
                o.created_at as CreatedAt,
                o.driver_accepted_at as DriverAcceptedAt,
                o.completed_at as CompletedAt
            FROM orders o
            WHERE o.id = @orderId AND o.driver_id = @driverId";
        
        try
        {
            var order = await _connection.QueryFirstOrDefaultAsync<DriverOrderViewModel>(sql, new { orderId, driverId });
            if (order != null)
            {
                order.StatusText = GetStatusText(order.Status);
            }
            return order;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting order detail {OrderId}", orderId);
            return null;
        }
    }
    
    public async Task<ServiceResult> AcceptOrderAsync(long orderId, long driverId)
    {
        await using var transaction = await _connection.BeginTransactionAsync();
        
        try
        {
            // ========== VALIDATION: Kiểm tra driver có đang busy không ==========
            // Check 1: Kiểm tra driver status
            var driverStatusSql = "SELECT status FROM drivers WHERE id = @driverId FOR UPDATE";
            var driverStatus = await _connection.QueryFirstOrDefaultAsync<string>(driverStatusSql, new { driverId }, transaction);
            
            if (driverStatus == null)
                return ServiceResult.Fail("Driver not found");
            
            if (driverStatus == "offline")
                return ServiceResult.Fail("Bạn đang offline. Vui lòng chuyển sang trạng thái sẵn sàng để nhận đơn.");
            
            // Check 2: Kiểm tra số đơn đang xử lý (status 2,3: Đang lấy đồ ăn, Đang giao hàng)
            // Đây là check chính xác nhất - kiểm tra thực tế có đơn đang xử lý không
            var activeOrdersSql = @"
                SELECT COUNT(*) 
                FROM orders 
                WHERE driver_id = @driverId AND status IN (2, 3)";
            
            var activeOrdersCount = await _connection.QueryFirstOrDefaultAsync<int>(activeOrdersSql, new { driverId }, transaction);
            
            // Nếu có đơn đang xử lý thì không cho nhận đơn mới
            if (activeOrdersCount > 0)
            {
                _logger.LogWarning("Driver {DriverId} tried to accept order {OrderId} but has {Count} active orders (status 2,3)", 
                    driverId, orderId, activeOrdersCount);
                return ServiceResult.Fail($"Bạn đang có {activeOrdersCount} đơn hàng đang xử lý. Vui lòng hoàn thành đơn hiện tại trước khi nhận đơn mới.");
            }
            
            // Nếu driver status = "busy" nhưng không có đơn active, reset về "available" (fix inconsistency)
            if (driverStatus == "busy" && activeOrdersCount == 0)
            {
                _logger.LogWarning("Driver {DriverId} status is 'busy' but has no active orders. Resetting to 'available'", driverId);
                var resetStatusSql = "UPDATE drivers SET status = 'available', updated_at = NOW() WHERE id = @driverId";
                await _connection.ExecuteAsync(resetStatusSql, new { driverId }, transaction);
            }
            
            // ========== VALIDATION: Kiểm tra order có available không ==========
            // Check order is available
            var checkSql = "SELECT status, driver_id FROM orders WHERE id = @orderId FOR UPDATE";
            var order = await _connection.QueryFirstOrDefaultAsync(checkSql, new { orderId }, transaction);
            
            if (order == null)
                return ServiceResult.Fail("Order not found");
            
            // Validation: Order phải ở status 1 (Chờ tài xế) - theo OrderStatusHelper
            if (order.status != 1)
                return ServiceResult.Fail("Đơn hàng không khả dụng. Chỉ có thể nhận đơn ở trạng thái 'Chờ tài xế' (status = 1)");
            
            if (order.driver_id != null)
                return ServiceResult.Fail("Order already assigned to another driver");
            
            // ========== ACCEPT ORDER ==========
            // Update order
            var updateOrderSql = @"
                UPDATE orders 
                SET driver_id = @driverId, 
                    driver_accepted_at = NOW(),
                    status = 2,
                    updated_at = NOW()
                WHERE id = @orderId";
            
            await _connection.ExecuteAsync(updateOrderSql, new { orderId, driverId }, transaction);
            
            // Update driver status to busy (chỉ khi nhận đơn thành công)
            var updateDriverSql = @"
                UPDATE drivers 
                SET status = 'busy', updated_at = NOW()
                WHERE id = @driverId";
            
            await _connection.ExecuteAsync(updateDriverSql, new { driverId }, transaction);
            
            _logger.LogInformation("Driver {DriverId} status updated to 'busy' after accepting order {OrderId}", driverId, orderId);
            
            // Log status change
            await LogStatusChangeAsync(orderId, 1, 2, driverId, "driver", "Driver accepted order", transaction);
            
            await transaction.CommitAsync();
            
            _logger.LogInformation("Driver {DriverId} accepted order {OrderId}", driverId, orderId);
            
            // 🔔 Gửi SignalR notification
            await SendOrderStatusNotification(orderId, 2, "Tài xế đã nhận đơn hàng");
            
            return ServiceResult.Ok("Order accepted successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error accepting order {OrderId} by driver {DriverId}", orderId, driverId);
            return ServiceResult.Fail("Failed to accept order");
        }
    }
    
    public async Task<ServiceResult> StartPickupAsync(long orderId, long driverId)
    {
        return await UpdateOrderStatusAsync(orderId, driverId, 2, 3, "Started pickup");
    }
    
    public async Task<ServiceResult> StartDeliveryAsync(long orderId, long driverId)
    {
        // Start delivery: status 2 (Đang lấy đồ ăn) → 3 (Đang giao hàng) - theo OrderStatusHelper
        return await UpdateOrderStatusAsync(orderId, driverId, 2, 3, "Bắt đầu giao hàng");
    }
    
    public async Task<ServiceResult> CompleteOrderAsync(long orderId, long driverId)
    {
        await using var transaction = await _connection.BeginTransactionAsync();
        
        try
        {
            // Check order status
            var checkSql = "SELECT status, driver_id FROM orders WHERE id = @orderId FOR UPDATE";
            var order = await _connection.QueryFirstOrDefaultAsync(checkSql, new { orderId }, transaction);
            
            if (order == null)
                return ServiceResult.Fail("Order not found");
            
            if (order.driver_id != driverId)
                return ServiceResult.Fail("You are not assigned to this order");
            
            // Validation: Order phải ở status 3 (Đang giao hàng) - theo OrderStatusHelper
            if (order.status != 3)
                return ServiceResult.Fail("Đơn hàng không ở trạng thái 'Đang giao hàng'. Chỉ có thể hoàn thành đơn khi đang giao (status = 3)");
            
            // Update order: status 3 → 4 (Đang giao hàng → Hoàn thành) - theo OrderStatusHelper
            var updateOrderSql = @"
                UPDATE orders 
                SET status = 4,
                    completed_at = NOW(),
                    updated_at = NOW()
                WHERE id = @orderId";
            
            await _connection.ExecuteAsync(updateOrderSql, new { orderId }, transaction);
            
            // Update driver
            var updateDriverSql = @"
                UPDATE drivers 
                SET status = 'available',
                    total_orders = total_orders + 1,
                    updated_at = NOW()
                WHERE id = @driverId";
            
            await _connection.ExecuteAsync(updateDriverSql, new { driverId }, transaction);
            
            // Log status change: 3 → 4
            await LogStatusChangeAsync(orderId, 3, 4, driverId, "driver", "Đơn hàng đã hoàn thành", transaction);
            
            await transaction.CommitAsync();
            
            _logger.LogInformation("Driver {DriverId} completed order {OrderId}", driverId, orderId);
            
            // 🔔 Gửi SignalR notification
            await SendOrderStatusNotification(orderId, 4, "Đơn hàng đã hoàn thành");
            
            return ServiceResult.Ok("Order completed successfully");
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error completing order {OrderId}", orderId);
            return ServiceResult.Fail("Failed to complete order");
        }
    }
    
    public async Task<ServiceResult> UpdateStatusAsync(long driverId, string status)
    {
        if (!new[] { "available", "offline", "busy" }.Contains(status.ToLower()))
            return ServiceResult.Fail("Invalid status");
        
        try
        {
            var sql = @"
                UPDATE drivers 
                SET status = @status, updated_at = NOW()
                WHERE id = @driverId";
            
            await _connection.ExecuteAsync(sql, new { driverId, status });
            
            _logger.LogInformation("Driver {DriverId} status updated to {Status}", driverId, status);
            
            return ServiceResult.Ok($"Status updated to {status}");
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating driver status");
            return ServiceResult.Fail("Failed to update status");
        }
    }
    
    public async Task<DriverStatsViewModel> GetDriverStatsAsync(long driverId)
    {
        if (_connection.State != System.Data.ConnectionState.Open)
        {
            await _connection.OpenAsync();
        }
        
        try
        {
            // Bước 1: Lấy thông tin cơ bản của driver (không join với orders)
            // Dùng lowercase alias để tránh vấn đề phân biệt hoa thường của PostgreSQL
            // Dapper sẽ tự động map lowercase alias vào PascalCase properties của DriverStatsViewModel
            var driverSql = @"
                SELECT 
                    d.id as driverid,
                    d.full_name as fullname,
                    d.status as status,
                    d.rating as rating,
                    d.total_orders as totalorders
                FROM drivers d
                WHERE d.id = @driverId";
            
            var stats = await _connection.QueryFirstOrDefaultAsync<DriverStatsViewModel>(driverSql, new { driverId });
            
            // Nếu không tìm thấy driver, trả về object mặc định
            if (stats == null)
            {
                _logger.LogWarning("Driver {DriverId} not found", driverId);
                return new DriverStatsViewModel { DriverId = driverId };
            }
            
            // Bước 2: Tính toán stats từ orders (riêng biệt để tránh lỗi GROUP BY khi không có đơn)
            // Dùng lowercase alias để tránh vấn đề phân biệt hoa thường của PostgreSQL
            var ordersStatsSql = @"
                SELECT 
                    COUNT(CASE WHEN o.status IN (2,3) THEN 1 END) as pendingorders,
                    COUNT(
                        CASE 
                            WHEN o.driver_accepted_at IS NOT NULL 
                                AND DATE(o.driver_accepted_at) = CURRENT_DATE 
                                AND o.status IN (2,3,4)
                            THEN 1 
                        END
                    ) as todayorders,
                    COUNT(CASE WHEN o.status = 4 THEN 1 END) as completedorders,
                    MAX(CASE WHEN o.status = 4 THEN o.completed_at END) as lastordercompletedat
                FROM orders o
                WHERE o.driver_id = @driverId";
            
            var ordersStats = await _connection.QueryFirstOrDefaultAsync<dynamic>(ordersStatsSql, new { driverId });

            // Gán stats từ orders (sẽ là 0 nếu driver chưa có đơn nào)
            // Truy cập bằng lowercase để match với PostgreSQL alias
            stats.PendingOrders = (int?)(ordersStats?.pendingorders ?? ordersStats?.PendingOrders) ?? 0;
            stats.TodayOrders = (int?)(ordersStats?.todayorders ?? ordersStats?.TodayOrders) ?? 0;
            stats.CompletedOrders = (int?)(ordersStats?.completedorders ?? ordersStats?.CompletedOrders) ?? 0;
            stats.LastOrderCompletedAt = ordersStats?.lastordercompletedat ?? ordersStats?.LastOrderCompletedAt;
            
            // Bước 3: Tính toán thu nhập hôm nay (nếu có hoa hồng từ đơn hoàn thành hôm nay)
            // Giả sử tài xế nhận 10% hoa hồng từ tổng giá trị đơn hàng
            var todayEarningsSql = @"
                SELECT COALESCE(SUM(o.total_price * 0.1), 0)::int as earnings
                FROM orders o
                WHERE o.driver_id = @driverId 
                  AND o.status = 4 
                  AND DATE(o.completed_at) = CURRENT_DATE";
            
            var todayEarningsResult = await _connection.QueryFirstOrDefaultAsync<dynamic>(todayEarningsSql, new { driverId });
            stats.TodayEarnings = todayEarningsResult?.earnings ?? 0;
            
            // Bước 4: Tính toán thu nhập tháng này
            var monthEarningsSql = @"
                SELECT COALESCE(SUM(o.total_price * 0.1), 0)::int as earnings
                FROM orders o
                WHERE o.driver_id = @driverId 
                  AND o.status = 4 
                  AND DATE_TRUNC('month', o.completed_at) = DATE_TRUNC('month', CURRENT_DATE)";
            
            var monthEarningsResult = await _connection.QueryFirstOrDefaultAsync<dynamic>(monthEarningsSql, new { driverId });
            stats.MonthEarnings = monthEarningsResult?.earnings ?? 0;
            
            return stats;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting driver stats for driver {DriverId}", driverId);
            return new DriverStatsViewModel { DriverId = driverId };
        }
    }
    
    public async Task<Driver?> GetDriverByUserIdAsync(long userId)
    {
        var sql = "SELECT * FROM drivers WHERE user_id = @userId";
        
        try
        {
            return await _connection.QueryFirstOrDefaultAsync<Driver>(sql, new { userId });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting driver by user {UserId}", userId);
            return null;
        }
    }
    
    // Helper methods
    
    private async Task<ServiceResult> UpdateOrderStatusAsync(
        long orderId, 
        long driverId, 
        int expectedStatus, 
        int newStatus, 
        string notes)
    {
        await using var transaction = await _connection.BeginTransactionAsync();
        
        try
        {
            // Validate
            var checkSql = "SELECT status, driver_id FROM orders WHERE id = @orderId FOR UPDATE";
            var order = await _connection.QueryFirstOrDefaultAsync(checkSql, new { orderId }, transaction);
            
            if (order == null)
                return ServiceResult.Fail("Order not found");
            
            if (order.driver_id != driverId)
                return ServiceResult.Fail("You are not assigned to this order");
            
            if (order.status != expectedStatus)
                return ServiceResult.Fail($"Order is not in expected status");
            
            // Update
            var updateSql = @"
                UPDATE orders 
                SET status = @newStatus, updated_at = NOW()
                WHERE id = @orderId";
            
            await _connection.ExecuteAsync(updateSql, new { orderId, newStatus }, transaction);
            
            // Log
            await LogStatusChangeAsync(orderId, expectedStatus, newStatus, driverId, "driver", notes, transaction);
            
            await transaction.CommitAsync();
            
            // 🔔 Gửi SignalR notification sau khi commit
            await SendOrderStatusNotification(orderId, newStatus, notes);
            
            return ServiceResult.Ok(notes);
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error updating order status");
            return ServiceResult.Fail("Failed to update status");
        }
    }
      /// <summary>
    /// Lấy driver_id từ user_id
    /// </summary>
    public long? GetDriverIdByUserId(long userId)
    {
        var sql = "SELECT id FROM drivers WHERE user_id = @userId LIMIT 1";
        
        try
        {
            if (_connection.State != System.Data.ConnectionState.Open)
            {
                _connection.Open();
            }
            
            var driverId = _connection.QueryFirstOrDefault<long?>(sql, new { userId });
            return driverId;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting driver ID by user {UserId}", userId);
            return null;
        }
    }
    
    /// <summary>
    /// Ghi log thay đổi trạng thái đơn hàng vào order_status_history
    /// Dùng trực tiếp status theo OrderStatusHelper (1-6), không map
    /// </summary>
    private async Task LogStatusChangeAsync(
        long orderId, 
        int oldStatus, 
        int newStatus, 
        long changedByUserId,
        string changedByRole,
        string notes,
        NpgsqlTransaction transaction)
    {
        // Dùng trực tiếp status theo OrderStatusHelper (1-6)
        // order_status_history.status có thể có constraint, nhưng giả sử nó chấp nhận 1-6
        var sql = @"
            INSERT INTO order_status_history 
                (order_id, status, note, created_at)
            VALUES 
                (@orderId, @status, @notes, NOW())";
        
        await _connection.ExecuteAsync(sql, new 
        { 
            orderId,
            status = newStatus, // Dùng trực tiếp status, không map
            notes 
        }, transaction);
    }


    /// <summary>
    /// Lấy text hiển thị status theo OrderStatusHelper (1-6)
    /// </summary>
    private string GetStatusText(int status)
    {
        return OrderStatusHelper.GetStatusText(status);
    }
    
    /// <summary>
    /// Gửi SignalR notification khi trạng thái đơn hàng thay đổi
    /// </summary>
    private async Task SendOrderStatusNotification(long orderId, int newStatus, string message)
    {
        try
        {
            var groupName = $"Order_{orderId}";
            await _hubContext.Clients.Group(groupName).SendAsync("OrderStatusUpdated", new
            {
                orderId,
                status = newStatus,
                statusText = GetStatusText(newStatus),
                message,
                timestamp = DateTime.UtcNow
            });
            
            _logger.LogInformation("Sent SignalR notification for order {OrderId}, status {Status}", orderId, newStatus);
        }
        catch (Exception ex)
        {
            // Log lỗi nhưng không throw (để không ảnh hưởng logic chính)
            _logger.LogError(ex, "Failed to send SignalR notification for order {OrderId}", orderId);
        }
    }
}
