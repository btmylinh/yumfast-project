using System.ComponentModel.DataAnnotations;

namespace WebApp.Models;

/// <summary>
/// ViewModel request tạo thanh toán VNPay/Momo
/// </summary>
public class PaymentRequestViewModel
{
    [Required]
    public long OrderId { get; set; }
    
    [Required]
    public string PaymentMethod { get; set; } = string.Empty; // vnpay, momo
    
    [Required]
    public string ReturnUrl { get; set; } = string.Empty;
    
    public string? CancelUrl { get; set; }
    
    public string? IpAddress { get; set; }
}

/// <summary>
/// ViewModel response từ payment service
/// </summary>
public class PaymentResponseViewModel
{
    public bool Success { get; set; }
    public string? PaymentUrl { get; set; }
    public string? ErrorMessage { get; set; }
    public string? TransactionId { get; set; }
}
