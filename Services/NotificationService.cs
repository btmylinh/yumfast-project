using Microsoft.AspNetCore.SignalR;
using WebApp.Hubs;
using WebApp.Models;
using WebApp.Services.Interfaces;

namespace WebApp.Services;

/// <summary>
/// Service implementation cho Notification qua SignalR
/// </summary>
public class NotificationService : INotificationService
{
    private readonly IHubContext<OrderHub> _hubContext;
    private readonly ILogger<NotificationService> _logger;
    
    public NotificationService(
        IHubContext<OrderHub> hubContext,
        ILogger<NotificationService> logger)
    {
        _hubContext = hubContext;
        _logger = logger;
    }
    
    public async Task NotifyOrderStatusChangedAsync(long orderId, int status, string message)
    {
        try
        {
            var groupName = $"Order_{orderId}";
            
            await _hubContext.Clients.Group(groupName).SendAsync("OrderStatusUpdated", new
            {
                orderId,
                status,
                statusText = GetStatusText(status),
                message,
                timestamp = DateTime.Now
            });
            
            _logger.LogInformation("Sent status change notification for order {OrderId}, status {Status}", orderId, status);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending status notification for order {OrderId}", orderId);
        }
    }
    
    public async Task NotifyDriverAssignedAsync(long orderId, DriverInfoDto driverInfo)
    {
        try
        {
            var groupName = $"Order_{orderId}";
            
            await _hubContext.Clients.Group(groupName).SendAsync("DriverAssigned", new
            {
                orderId,
                driver = driverInfo,
                message = $"Driver {driverInfo.FullName} has been assigned to your order",
                timestamp = DateTime.Now
            });
            
            _logger.LogInformation("Sent driver assigned notification for order {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending driver assigned notification for order {OrderId}", orderId);
        }
    }
    
    public async Task NotifyOrderCancelledAsync(long orderId, string reason)
    {
        try
        {
            var groupName = $"Order_{orderId}";
            
            await _hubContext.Clients.Group(groupName).SendAsync("OrderCancelled", new
            {
                orderId,
                reason,
                message = "Your order has been cancelled",
                timestamp = DateTime.Now
            });
            
            _logger.LogInformation("Sent cancellation notification for order {OrderId}", orderId);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error sending cancellation notification for order {OrderId}", orderId);
        }
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
}
