namespace WebApp.Models;

/// <summary>
/// Model cho tài xế giao hàng
/// </summary>
public class Driver
{
    public long Id { get; set; }
    public long UserId { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    
    /// <summary>
    /// Status: offline, available, busy
    /// </summary>
    public string Status { get; set; } = "offline";
    
    public decimal Rating { get; set; } = 5.0m;
    public int TotalOrders { get; set; } = 0;
    
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public User? User { get; set; }
    public ICollection<Order>? Orders { get; set; }
    public ICollection<OrderReview>? Reviews { get; set; }
}
