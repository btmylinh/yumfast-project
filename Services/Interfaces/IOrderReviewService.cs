using WebApp.Models;

namespace WebApp.Services.Interfaces;

/// <summary>
/// Service quản lý đánh giá đơn hàng
/// </summary>
public interface IOrderReviewService
{
    /// <summary>
    /// Kiểm tra có thể review không (order completed, chưa review)
    /// </summary>
    Task<bool> CanReviewOrderAsync(long orderId, long userId);

    /// <summary>
    /// Tạo review mới
    /// </summary>
    Task<ServiceResult> CreateReviewAsync(CreateReviewViewModel dto, long userId);

    /// <summary>
    /// Lấy review của 1 đơn hàng
    /// </summary>
    Task<OrderReview?> GetOrderReviewAsync(long orderId);

    /// <summary>
    /// Lấy chi tiết đầy đủ review của order bao gồm cả product reviews
    /// </summary>
    Task<object?> GetOrderReviewDetailsAsync(long orderId);

    /// <summary>
    /// Lấy danh sách reviews của tài xế (với phân trang)
    /// </summary>
    Task<List<OrderReview>> GetDriverReviewsAsync(long driverId, int page = 1, int pageSize = 10);

    /// <summary>
    /// Admin trả lời review
    /// </summary>
    Task<ServiceResult> AdminReplyAsync(long reviewId, string reply, long adminId);
    Task<ServiceResult> AdminReplyByOrderIdAsync(long orderId, string reply, long adminId);
}
