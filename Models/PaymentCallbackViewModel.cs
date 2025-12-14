namespace WebApp.Models;

/// <summary>
/// ViewModel response từ VNPay/Momo callback
/// </summary>
public class PaymentCallbackViewModel
{
    public bool Success { get; set; }
    public long OrderId { get; set; }
    public string? TransactionId { get; set; }
    public int Amount { get; set; }
    public string? ErrorMessage { get; set; }
    public string? ResponseCode { get; set; }
    public DateTime? PaidAt { get; set; }
}
