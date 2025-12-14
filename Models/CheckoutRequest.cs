namespace WebApp.Models
{
    public class CheckoutRequest
    {
        public long AddressId { get; set; }
        public string? CouponCode { get; set; }
        public string PaymentMethod { get; set; } = "COD"; // COD|VNPAY|MOMO
    }
}

