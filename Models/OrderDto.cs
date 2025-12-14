namespace WebApp.Models
{
    public class OrderDto
    {
        public long Id { get; set; }
        public string Code { get; set; } = string.Empty;
        public long? UserId { get; set; }
        public string Email { get; set; } = string.Empty;
        public string Phone { get; set; } = string.Empty;
        public int TotalPrice { get; set; }
        public short PaymentStatus { get; set; }
        public short Status { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}

