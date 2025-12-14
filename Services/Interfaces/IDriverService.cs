using WebApp.Models;

namespace WebApp.Services.Interfaces;

/// <summary>
/// Service quản lý tài xế và đơn hàng của tài xế
/// </summary>
public interface IDriverService
{
    /// <summary>
    /// Lấy danh sách đơn hàng chưa có tài xế (status=1)
    /// </summary>
    Task<List<DriverOrderViewModel>> GetAvailableOrdersAsync();
    
    /// <summary>
    /// Lấy danh sách đơn hàng của tài xế (status=2,3,4)
    /// </summary>
    Task<List<DriverOrderViewModel>> GetMyOrdersAsync(long driverId);
    
    /// <summary>
    /// Lấy chi tiết 1 đơn hàng
    /// </summary>
    Task<DriverOrderViewModel?> GetOrderDetailAsync(long orderId, long driverId);
    
    /// <summary>
    /// Tài xế nhận đơn (status: 1→2, cập nhật driver_id)
    /// </summary>
    Task<ServiceResult> AcceptOrderAsync(long orderId, long driverId);
    
    /// <summary>
    /// Bắt đầu giao hàng (status: 2→3)
    /// </summary>
    Task<ServiceResult> StartDeliveryAsync(long orderId, long driverId);
    
    /// <summary>
    /// Hoàn thành đơn (status: 3→4, cập nhật completed_at, tăng total_orders)
    /// </summary>
    Task<ServiceResult> CompleteOrderAsync(long orderId, long driverId);
    
    /// <summary>
    /// Đổi trạng thái tài xế (available/offline/busy)
    /// </summary>
    Task<ServiceResult> UpdateStatusAsync(long driverId, string status);
    
    /// <summary>
    /// Lấy thống kê tài xế
    /// </summary>
    Task<DriverStatsViewModel> GetDriverStatsAsync(long driverId);
    
    /// <summary>
    /// Lấy thông tin driver theo userId
    /// </summary>
    Task<Driver?> GetDriverByUserIdAsync(long userId);
    
    /// <summary>
    /// Lấy driver_id từ user_id
    /// </summary>
    long? GetDriverIdByUserId(long userId);
}

/// <summary>
/// Result trả về từ service operations
/// </summary>
public class ServiceResult
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public object? Data { get; set; }
    
    public static ServiceResult Ok(string message = "Success", object? data = null)
        => new() { Success = true, Message = message, Data = data };
    
    public static ServiceResult Fail(string message)
        => new() { Success = false, Message = message };
}
