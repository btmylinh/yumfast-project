namespace WebApp.Models
{
    public class Product
    {
        public long Id { get; set; }
        public long CategoryId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Price { get; set; } // Giá bán (VND)
        public string[]? Images { get; set; } // JSON array of image paths
        public short Status { get; set; } = 1; // 0=inactive, 1=active
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Category? Category { get; set; }
        public ICollection<ProductOption> Options { get; set; } = new List<ProductOption>();
        public ICollection<ProductStock> Stock { get; set; } = new List<ProductStock>();
        public ICollection<ProductReview> Reviews { get; set; } = new List<ProductReview>();
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}

