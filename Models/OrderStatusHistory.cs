namespace WebApp.Models
{
    public class OrderStatusHistory
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public short Status { get; set; } // 0=placed, 1=shipping, 2=paid, 3=completed, 4=cancelled
        public string? Note { get; set; }
        public long? CreatedBy { get; set; } // User ID (admin hoặc system)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Order? Order { get; set; }
    }
}

