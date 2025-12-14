namespace WebApp.Models
{
    public class UserAddress
    {
        public long Id { get; set; }
        public long UserId { get; set; }
        public long ZoneId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public string Address { get; set; } = string.Empty;
        public string? Ward { get; set; }
        public string? District { get; set; }
        public string? City { get; set; }
        public bool IsDefault { get; set; } = false;
        public short Status { get; set; } = 1; // 0=inactive, 1=active
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public User? User { get; set; }
        public ShippingZone? Zone { get; set; }
    }
}

