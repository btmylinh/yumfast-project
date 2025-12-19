namespace WebApp.Models;

/// <summary>
/// Thống kê reviews của sản phẩm
/// </summary>
public class ProductReviewStats
{
    public long ProductId { get; set; }
    
    /// <summary>
    /// Tổng số reviews đã approved
    /// </summary>
    public int TotalReviews { get; set; }
    
    /// <summary>
    /// Rating trung bình (1-5)
    /// </summary>
    public decimal AverageRating { get; set; }
    
    /// <summary>
    /// Phân bố rating: Key = rating (1-5), Value = số lượng reviews
    /// </summary>
    public Dictionary<int, int> RatingDistribution { get; set; } = new Dictionary<int, int>();
}

