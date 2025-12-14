namespace WebApp.Models;

/// <summary>
/// Model log trạng thái đơn hàng
/// </summary>
public class OrderStatusLog
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    
    public int? OldStatus { get; set; }
    public int NewStatus { get; set; }
    
    public string? Notes { get; set; }
    public long? ChangedByUserId { get; set; }
    
    /// <summary>
    /// Role của người thay đổi: user, driver, admin, system
    /// </summary>
    public string? ChangedByRole { get; set; }
    
    public DateTime ChangedAt { get; set; }
    
    // Navigation properties
    public Order? Order { get; set; }
    public User? ChangedByUser { get; set; }
}
