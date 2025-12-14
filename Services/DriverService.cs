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
    /// Lấy danh sách đơn hàng của tài xế (status 2,3,4: Đang lấy, Đang giao, Hoàn thành)
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
            WHERE o.driver_id = @driverId AND o.status IN (2, 3, 4)
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
            // Check order is available
            var checkSql = "SELECT status, driver_id FROM orders WHERE id = @orderId FOR UPDATE";
            var order = await _connection.QueryFirstOrDefaultAsync(checkSql, new { orderId }, transaction);
            
            if (order == null)
                return ServiceResult.Fail("Order not found");
            
            if (order.status != 1)
                return ServiceResult.Fail("Order is not available");
            
            if (order.driver_id != null)
                return ServiceResult.Fail("Order already assigned to another driver");
            
            // Update order
            var updateOrderSql = @"
                UPDATE orders 
                SET driver_id = @driverId, 
                    driver_accepted_at = NOW(),
                    status = 2,
                    updated_at = NOW()
                WHERE id = @orderId";
            
            await _connection.ExecuteAsync(updateOrderSql, new { orderId, driverId }, transaction);
            
            // Update driver status
            var updateDriverSql = @"
                UPDATE drivers 
                SET status = 'busy', updated_at = NOW()
                WHERE id = @driverId";
            
            await _connection.ExecuteAsync(updateDriverSql, new { driverId }, transaction);
            
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
        return await UpdateOrderStatusAsync(orderId, driverId, 3, 4, "Started delivery");
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
            
            if (order.status != 4)
                return ServiceResult.Fail("Order is not in delivering status");
            
            // Update order
            var updateOrderSql = @"
                UPDATE orders 
                SET status = 5,
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
            
            // Log status change
            await LogStatusChangeAsync(orderId, 4, 5, driverId, "driver", "Order completed", transaction);
            
            await transaction.CommitAsync();
            
            _logger.LogInformation("Driver {DriverId} completed order {OrderId}", driverId, orderId);
            
            // 🔔 Gửi SignalR notification
            await SendOrderStatusNotification(orderId, 5, "Đơn hàng đã hoàn thành");
            
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
        var sql = @"
            SELECT 
                d.id as DriverId,
                d.full_name as FullName,
                d.status as Status,
                d.rating as Rating,
                d.total_orders as TotalOrders,
                COUNT(CASE WHEN o.status IN (2,3,4) THEN 1 END) as PendingOrders,
                COUNT(CASE WHEN o.status = 5 AND DATE(o.completed_at) = CURRENT_DATE THEN 1 END) as TodayOrders,
                COUNT(CASE WHEN o.status = 5 THEN 1 END) as CompletedOrders,
                MAX(CASE WHEN o.status = 5 THEN o.completed_at END) as LastOrderCompletedAt
            FROM drivers d
            LEFT JOIN orders o ON o.driver_id = d.id
            WHERE d.id = @driverId
            GROUP BY d.id";
        
        try
        {
            var stats = await _connection.QueryFirstOrDefaultAsync<DriverStatsViewModel>(sql, new { driverId });
            return stats ?? new DriverStatsViewModel { DriverId = driverId };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error getting driver stats");
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
        // Bảng order_status_history có cấu trúc: order_id, status, note, created_at
        var sql = @"
            INSERT INTO order_status_history 
                (order_id, status, note, created_at)
            VALUES 
                (@orderId, @newStatus, @notes, NOW())";
        
        await _connection.ExecuteAsync(sql, new 
        { 
            orderId, 
            newStatus, 
            notes 
        }, transaction);
    }
    
    private string GetStatusText(int status)
    {
        return status switch
        {
            0 => "Pending",
            1 => "Confirmed",
            2 => "Driver Assigned",
            3 => "Picking Up",
            4 => "Delivering",
            5 => "Completed",
            6 => "Cancelled",
            7 => "Refunded",
            _ => "Unknown"
        };
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
