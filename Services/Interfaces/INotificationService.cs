using WebApp.Models;

namespace WebApp.Services.Interfaces;

/// <summary>
/// Service gửi notification realtime qua SignalR
/// </summary>
public interface INotificationService
{
    /// <summary>
    /// Thông báo khi status đơn hàng thay đổi
    /// </summary>
    Task NotifyOrderStatusChangedAsync(long orderId, int status, string message);
    
    /// <summary>
    /// Thông báo khi driver được assign
    /// </summary>
    Task NotifyDriverAssignedAsync(long orderId, DriverInfoDto driverInfo);
    
    /// <summary>
    /// Thông báo khi đơn hàng bị hủy
    /// </summary>
    Task NotifyOrderCancelledAsync(long orderId, string reason);
}
