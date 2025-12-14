using System.Text.Json;

namespace WebApp.Models;

/// <summary>
/// Model chi tiết giao dịch thanh toán (VNPay, Momo, COD)
/// </summary>
public class PaymentTransaction
{
    public long Id { get; set; }
    public long OrderId { get; set; }
    public long? PaymentId { get; set; }
    
    /// <summary>
    /// Payment method: vnpay, momo, cod
    /// </summary>
    public string PaymentMethod { get; set; } = string.Empty;
    
    /// <summary>
    /// Transaction ID từ payment gateway
    /// </summary>
    public string? TransactionId { get; set; }
    
    public int Amount { get; set; }
    public string Currency { get; set; } = "VND";
    
    /// <summary>
    /// Status: pending, processing, success, failed, refunded
    /// </summary>
    public string Status { get; set; } = "pending";
    
    /// <summary>
    /// JSON request gửi đến payment gateway
    /// </summary>
    public JsonDocument? RequestData { get; set; }
    
    /// <summary>
    /// JSON response nhận từ payment gateway
    /// </summary>
    public JsonDocument? ResponseData { get; set; }
    
    /// <summary>
    /// JSON callback data (IPN)
    /// </summary>
    public JsonDocument? CallbackData { get; set; }
    
    public string? ErrorMessage { get; set; }
    
    public DateTime? PaidAt { get; set; }
    public DateTime? RefundedAt { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
    
    // Navigation properties
    public Order? Order { get; set; }
    public OrderPayment? Payment { get; set; }
}
