namespace WebApp.Models
{
    public class OrderPayment
    {
        public long Id { get; set; }
        public long OrderId { get; set; }
        public int Amount { get; set; } // VND
        public string Currency { get; set; } = "VND";
        public short Status { get; set; } = 0; // 0=pending, 1=completed, 2=failed
        public string Method { get; set; } = "COD"; // COD, VNPAY, MOMO
        public string? ProviderTxnId { get; set; } // Transaction ID từ VNPay/MoMo
        public DateTime? PaidAt { get; set; }
        public Dictionary<string, object>? Metadata { get; set; } // JSON: extra data
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime UpdatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Order? Order { get; set; }
    }
}

