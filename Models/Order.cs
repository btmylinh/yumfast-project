namespace WebApp.Models
{
    public class Order
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty; // ORD-20251211-001
        public long? UserId { get; set; }
        public long? CouponId { get; set; }
        public string ShipName { get; set; } = string.Empty;
        public string ShipPhone { get; set; } = string.Empty;
        public string ShipAddressText { get; set; } = string.Empty;
        public int PriceSubtotal { get; set; } = 0; // Tổng giá hàng
        public int PriceDiscount { get; set; } = 0; // Giảm giá
        public int PriceShipping { get; set; } = 0; // Phí vận chuyển
        public int TotalPrice { get; set; } = 0; // Tổng cộng
        public short PaymentStatus { get; set; } = 0; // 0=pending, 1=completed, 2=failed
        public short Status { get; set; } = 0; // 0=pending, 1=confirmed, 2=driver_assigned, 3=picking_up, 4=delivering, 5=completed, 6=cancelled, 7=refunded
        public string? Note { get; set; }
        
        // Driver related fields
        public long? DriverId { get; set; }
        public DateTime? DriverAcceptedAt { get; set; }
        public DateTime? CompletedAt { get; set; }
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User? User { get; set; }
        public Driver? Driver { get; set; }
        public OrderReview? Review { get; set; }
        public ICollection<OrderItem> Items { get; set; } = new List<OrderItem>();
        public ICollection<OrderPayment> Payments { get; set; } = new List<OrderPayment>();
        public ICollection<OrderStatusHistory> StatusHistories { get; set; } = new List<OrderStatusHistory>();
        public ICollection<PaymentTransaction> PaymentTransactions { get; set; } = new List<PaymentTransaction>();
        public ICollection<OrderStatusLog> StatusLogs { get; set; } = new List<OrderStatusLog>();
    }
}

