using WebApp.Models;

namespace WebApp.Services.Interfaces;

/// <summary>
/// Service quản lý đánh giá sản phẩm
/// </summary>
public interface IProductReviewService
{
    /// <summary>
    /// Kiểm tra user có thể review sản phẩm không (đã mua, chưa review)
    /// </summary>
    Task<bool> CanReviewProductAsync(long productId, long userId);
    
    /// <summary>
    /// Tạo review mới cho sản phẩm
    /// </summary>
    Task<ServiceResult> CreateReviewAsync(CreateProductReviewViewModel dto, long userId);
    
    /// <summary>
    /// Lấy review của user cho sản phẩm cụ thể
    /// </summary>
    Task<ProductReview?> GetProductReviewAsync(long productId, long userId);
    
    /// <summary>
    /// Lấy danh sách reviews của sản phẩm (chỉ approved, có phân trang)
    /// </summary>
    Task<List<ProductReview>> GetProductReviewsAsync(long productId, int page = 1, int pageSize = 10);
    
    /// <summary>
    /// Admin approve/reject review (0=pending, 1=approved)
    /// </summary>
    Task<ServiceResult> UpdateReviewStatusAsync(long reviewId, short status, long adminId);
    
    /// <summary>
    /// User xóa review của mình
    /// </summary>
    Task<ServiceResult> DeleteReviewAsync(long reviewId, long userId);
    
    /// <summary>
    /// Lấy thống kê reviews của sản phẩm (average rating, total, distribution)
    /// </summary>
    Task<ProductReviewStats> GetProductReviewStatsAsync(long productId);
}

