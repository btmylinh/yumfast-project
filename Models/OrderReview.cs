namespace WebApp.Models;

/// <summary>
/// Model đánh giá đơn hàng
/// </summary>
public class OrderReview
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long UserId { get; set; }
    public long? DriverId { get; set; }
    
    /// <summary>
    /// Đánh giá đơn hàng (1-5 sao)
    /// </summary>
    public int OrderRating { get; set; }
    
    /// <summary>
    /// Đánh giá tài xế (1-5 sao)
    /// </summary>
    public int? DriverRating { get; set; }
    
    public string? Comment { get; set; }
    
    /// <summary>
    /// Array of image URLs
    /// </summary>
    public string[]? Images { get; set; }
    
    public string? AdminReply { get; set; }
    public DateTime? AdminRepliedAt { get; set; }
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Order? Order { get; set; }
    public User? User { get; set; }
    public Driver? Driver { get; set; }
}
