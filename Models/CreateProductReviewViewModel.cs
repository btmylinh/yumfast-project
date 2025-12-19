namespace WebApp.Models;

/// <summary>
/// ViewModel để tạo đánh giá sản phẩm
/// </summary>
public class CreateProductReviewViewModel
{
    public long ProductId { get; set; }
    
    /// <summary>
    /// Rating từ 1-5 sao
    /// </summary>
    public short Rating { get; set; }
    
    /// <summary>
    /// Comment (optional)
    /// </summary>
    public string? Comment { get; set; }
}

