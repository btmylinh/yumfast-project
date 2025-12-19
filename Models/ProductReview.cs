namespace WebApp.Models
{
    public class ProductReview
    {
        public long Id { get; set; }
        public long ProductId { get; set; }
        public long UserId { get; set; }
        public short Rating { get; set; } // 1-5 stars
        public string? Comment { get; set; }
        public string[]? Images { get; set; } // Array of image URLs
        public short Status { get; set; } = 1; // 0=pending, 1=approved
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;
        public long? OrderId { get; set; }

        // Navigation properties
        public Product? Product { get; set; }
        public User? User { get; set; }
    }
}

