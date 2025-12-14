using WebApp.Models;

namespace WebApp.Services.Interfaces;

/// <summary>
/// Service xử lý thanh toán VNPay và Momo
/// </summary>
public interface IPaymentService
{
    // VNPay Methods
    Task<PaymentResponseViewModel> CreateVNPayPaymentAsync(long orderId, string returnUrl, string ipAddress);
    Task<PaymentCallbackViewModel> ProcessVNPayReturnAsync(Dictionary<string, string> queryParams);
    Task<PaymentCallbackViewModel> ProcessVNPayIPNAsync(Dictionary<string, string> queryParams);
    
    // Momo Methods
    Task<PaymentResponseViewModel> CreateMomoPaymentAsync(long orderId, string returnUrl, string ipAddress);
    Task<PaymentCallbackViewModel> ProcessMomoReturnAsync(Dictionary<string, string> postData);
    Task<PaymentCallbackViewModel> ProcessMomoIPNAsync(Dictionary<string, string> postData);
    
    // Common Methods
    Task<bool> ProcessPaymentSuccessAsync(long orderId, string transactionId, string paymentMethod);
    Task<bool> ProcessPaymentFailureAsync(long orderId, string errorMessage);
    Task<ServiceResult> RefundPaymentAsync(long orderId, string reason);
}
