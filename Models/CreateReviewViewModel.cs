using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;

/// <summary>
/// ViewModel tạo đánh giá đơn hàng
/// </summary>
public class CreateReviewViewModel
{
    [Required]
    public long OrderId { get; set; }
    
    [Required]
    [Range(1, 5, ErrorMessage = "Order rating must be between 1 and 5")]
    public int OrderRating { get; set; }
    
    [Range(1, 5, ErrorMessage = "Driver rating must be between 1 and 5")]
    public int? DriverRating { get; set; }
    
    [MaxLength(1000, ErrorMessage = "Comment must not exceed 1000 characters")]
    public string? Comment { get; set; }
    
    [MaxLength(5, ErrorMessage = "Maximum 5 images allowed")]
    public List<string>? Images { get; set; }
}

/// <summary>
/// ViewModel response sau khi tạo review
/// </summary>
public class ReviewResponseViewModel
{
    public bool Success { get; set; }
    public string Message { get; set; } = string.Empty;
    public long? ReviewId { get; set; }
}
