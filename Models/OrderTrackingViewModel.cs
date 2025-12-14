namespace WebApp.Models;

/// <summary>
/// ViewModel tracking realtime cho user
/// </summary>
public class OrderTrackingViewModel
{
    public long OrderId { get; set; }
    public string OrderCode { get; set; } = string.Empty;
    public int Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    
    // Customer info
    public string ShipName { get; set; } = string.Empty;
    public string ShipPhone { get; set; } = string.Empty;
    public string ShipAddress { get; set; } = string.Empty;
    
    // Driver info
    public DriverInfoDto? Driver { get; set; }
    
    // Prices
    public int Subtotal { get; set; }
    public int Discount { get; set; }
    public int ShippingFee { get; set; }
    public int TotalPrice { get; set; }
    
    // Payment
    public string PaymentMethod { get; set; } = string.Empty;
    public int PaymentStatus { get; set; }
    
    // Timestamps
    public DateTime CreatedAt { get; set; }
    public DateTime? DriverAcceptedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    
    // Can actions
    public bool CanCancel { get; set; }
    public bool CanReview { get; set; }
    
    // History
    public List<OrderStatusLogDto> History { get; set; } = new();
    
    // Items
    public List<OrderItemDto> Items { get; set; } = new();
}

public class DriverInfoDto
{
    public long Id { get; set; }
    public string FullName { get; set; } = string.Empty;
    public string Phone { get; set; } = string.Empty;
    public decimal Rating { get; set; }
    public int TotalOrders { get; set; }
}

public class OrderStatusLogDto
{
    public int Status { get; set; }
    public string StatusText { get; set; } = string.Empty;
    public string? Notes { get; set; }
    public string? ChangedByRole { get; set; }
    public DateTime ChangedAt { get; set; }
}
