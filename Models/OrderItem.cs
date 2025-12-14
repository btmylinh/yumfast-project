namespace WebApp.Models
{
    public class OrderItem
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public long ProductId { get; set; }
        public int Quantity { get; set; }
        public int Price { get; set; } // Đơn giá tại thời điểm đặt
        public int Total { get; set; } // Tổng = Price * Quantity
        public Dictionary<string, object>? Options { get; set; } // JSON: {"size": "L", "topping": "cheese"}

        // Navigation properties
        public Order? Order { get; set; }
        public Product? Product { get; set; }
    }
}

