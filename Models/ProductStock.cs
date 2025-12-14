namespace WebApp.Models
{
    public class ProductStock
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public int Quantity { get; set; } = 0; // Tồn kho thực
        public int Reserved { get; set; } = 0; // Đã giữ (pending orders)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Product? Product { get; set; }

        // Computed property
        public int Available => Quantity - Reserved;
    }
}

