namespace WebApp.Models
{
    public class ProductOption
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public string Name { get; set; } = string.Empty; // "Size", "Topping"
        public string? Type { get; set; } // "select", "checkbox", "radio"
        public int Price { get; set; } = 0; // Giá thêm (nếu có)
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Product? Product { get; set; }
    }
}

