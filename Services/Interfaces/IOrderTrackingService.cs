using WebApp.Models;

namespace WebApp.Services.Interfaces;

/// <summary>
/// Service theo dõi trạng thái đơn hàng
/// </summary>
public interface IOrderTrackingService
{
    /// <summary>
    /// Lấy thông tin tracking đơn hàng (validate user chỉ xem đơn mình)
    /// </summary>
    Task<OrderTrackingViewModel?> GetOrderTrackingAsync(long orderId, long? userId = null);
    
    /// <summary>
    /// Lấy timeline lịch sử trạng thái từ order_status_logs
    /// </summary>
    Task<List<OrderStatusLogDto>> GetOrderHistoryAsync(long orderId);
    
    /// <summary>
    /// Admin update status thủ công
    /// </summary>
    Task<ServiceResult> UpdateOrderStatusAsync(long orderId, int newStatus, string? notes, long userId);
    
    /// <summary>
    /// Kiểm tra có thể hủy đơn không (status nhỏ hơn 3)
    /// </summary>
    Task<bool> CanCancelOrderAsync(long orderId, long userId);
    
    /// <summary>
    /// Hủy đơn (→6, release inventory, refund nếu đã thanh toán)
    /// </summary>
    Task<ServiceResult> CancelOrderAsync(long orderId, long userId, string reason);
    
    /// <summary>
    /// Gửi notification realtime qua SignalR
    /// </summary>
    Task SendStatusNotificationAsync(long orderId, int status);
}
