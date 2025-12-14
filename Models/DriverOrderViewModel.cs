namespace WebApp.Models;

/// <summary>
/// ViewModel hiển thị đơn hàng cho tài xế
/// </summary>
public class DriverOrderViewModel
{
    public long OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public int TotalPrice { get; set; }
    
    // Customer info
    public string ShipName { get; set; } = string.Empty;
    public string ShipPhone { get; set; } = string.Empty;
    public string ShipAddress { get; set; } = string.Empty;
    
    // Payment
    public string PaymentMethod { get; set; } = string.Empty;
    public int PaymentStatus { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? DriverAcceptedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Items
    public List<OrderItemDto> Items { get; set; } = new();
}

public class OrderItemDto
{
    public long ProductId { get; set; }
    public string ProductName { get; set; } = string.Empty;
    public string? ProductImage { get; set; }
    public int Quantity { get; set; }
    public int Price { get; set; }
    public int Total { get; set; }
}
