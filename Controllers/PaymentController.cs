using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using WebApp.Services;
using WebApp.Models;

namespace WebApp.Controllers;

/// <summary>
/// API Controller xử lý thanh toán VNPay và Momo
/// </summary>
[ApiController]
[Route("api/[controller]")]
public class PaymentController : ControllerBase
{
    private readonly VNPayService _vnpayService;
    private readonly MoMoService _momoService;
    private readonly ILogger<PaymentController> _logger;

    public PaymentController(
        VNPayService vnpayService,
        MoMoService momoService,
        ILogger<PaymentController> logger)
    {
        _vnpayService = vnpayService;
        _momoService = momoService;
        _logger = logger;
    }

    /// <summary>
    /// Tạo URL thanh toán VNPay và redirect user đến trang thanh toán
    /// POST /api/payment/vnpay/create
    /// Body: { "orderId": 123, "returnUrl": "https://..." }
    /// </summary>
    [HttpPost("vnpay/create")]
    [Authorize] // Yêu cầu đăng nhập
    public async Task<IActionResult> CreateVNPayPayment([FromBody] CreatePaymentRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập" });
            }

            // Validate request
            if (request.OrderId <= 0)
            {
                return BadRequest(new { success = false, message = "Order ID không hợp lệ" });
            }

            if (string.IsNullOrWhiteSpace(request.ReturnUrl))
            {
                return BadRequest(new { success = false, message = "Return URL không hợp lệ" });
            }

            // Lấy IP address của user
            var ipAddress = HttpContext.Connection.RemoteIpAddress?.ToString() ?? "127.0.0.1";

            // Tạo payment URL từ VNPay
            var result = await _vnpayService.CreateVNPayPaymentAsync(
                request.OrderId,
                request.ReturnUrl,
                ipAddress
            );

            if (!result.Success)
            {
                return BadRequest(new { 
                    success = false, 
                    message = result.ErrorMessage ?? "Không thể tạo thanh toán" 
                });
            }

            _logger.LogInformation("User {UserId} tạo VNPay payment cho đơn {OrderId}", 
                userId.Value, request.OrderId);

            return Ok(new { 
                success = true, 
                data = new {
                    paymentUrl = result.PaymentUrl,
                    transactionId = result.TransactionId
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo VNPay payment cho đơn {OrderId}", request.OrderId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Xử lý khi user quay về từ trang VNPay (success hoặc fail)
    /// GET /api/payment/vnpay/return với query parameters từ VNPay
    /// </summary>
    [HttpGet("vnpay/return")]
    [AllowAnonymous] // Không cần đăng nhập vì VNPay redirect
    public async Task<IActionResult> VNPayReturn()
    {
        try
        {
            // Lấy tất cả query params từ VNPay
            var queryParams = Request.Query.ToDictionary(
                kv => kv.Key, 
                kv => kv.Value.ToString()
            );

            if (!queryParams.Any())
            {
                return BadRequest(new { success = false, message = "Không có dữ liệu từ VNPay" });
            }

            // Xử lý callback
            var result = await _vnpayService.ProcessVNPayReturnAsync(queryParams);

            if (result.Success)
            {
                _logger.LogInformation("VNPay payment thành công cho đơn {OrderId}", result.OrderId);
                
                // TODO: Redirect đến trang order success
                return Redirect($"/orders/{result.OrderId}/success?txn={result.TransactionId}");
            }
            else
            {
                _logger.LogWarning("VNPay payment thất bại cho đơn {OrderId}. Lỗi: {Error}", 
                    result.OrderId, result.ErrorMessage);
                
                // TODO: Redirect đến trang payment failed
                return Redirect($"/orders/{result.OrderId}/payment-failed?error={result.ErrorMessage}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý VNPay return");
            return Redirect("/payment/error");
        }
    }

    /// <summary>
    /// Xử lý IPN (Instant Payment Notification) từ VNPay
    /// POST /api/payment/vnpay/ipn
    /// VNPay sẽ gọi endpoint này để confirm thanh toán
    /// </summary>
    [HttpPost("vnpay/ipn")]
    [HttpGet("vnpay/ipn")] // VNPay có thể dùng GET hoặc POST
    [AllowAnonymous]
    public async Task<IActionResult> VNPayIPN()
    {
        try
        {
            // Lấy tất cả query params từ VNPay IPN
            var queryParams = Request.Query.ToDictionary(
                kv => kv.Key, 
                kv => kv.Value.ToString()
            );

            if (!queryParams.Any())
            {
                return BadRequest(new { RspCode = "99", Message = "Invalid request" });
            }

            // Xử lý IPN
            var result = await _vnpayService.ProcessVNPayIPNAsync(queryParams);

            if (result.Success)
            {
                _logger.LogInformation("VNPay IPN thành công cho đơn {OrderId}", result.OrderId);
                
                // Response theo format VNPay yêu cầu
                return Ok(new { RspCode = "00", Message = "Confirm Success" });
            }
            else
            {
                _logger.LogWarning("VNPay IPN thất bại: {Error}", result.ErrorMessage);
                return Ok(new { RspCode = "99", Message = result.ErrorMessage });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý VNPay IPN");
            return Ok(new { RspCode = "99", Message = "System error" });
        }
    }

    /// <summary>
    /// Tạo URL thanh toán MoMo
    /// POST /api/payment/momo/create
    /// </summary>
    [HttpPost("momo/create")]
    [Authorize]
    public async Task<IActionResult> CreateMomoPayment([FromBody] CreatePaymentRequest request)
    {
        try
        {
            var userId = GetUserId();
            if (userId == null)
            {
                return Unauthorized(new { success = false, message = "Vui lòng đăng nhập" });
            }

            // Validate request
            if (request.OrderId <= 0)
            {
                return BadRequest(new { success = false, message = "Order ID không hợp lệ" });
            }

            if (string.IsNullOrWhiteSpace(request.ReturnUrl))
            {
                return BadRequest(new { success = false, message = "Return URL không hợp lệ" });
            }

            // Tạo notify URL (IPN endpoint)
            var notifyUrl = $"{Request.Scheme}://{Request.Host}/api/payment/momo/ipn";

            // Tạo payment URL từ MoMo
            var result = await _momoService.CreateMoMoPaymentAsync(
                request.OrderId,
                request.ReturnUrl,
                notifyUrl
            );

            if (!result.Success)
            {
                return BadRequest(new { 
                    success = false, 
                    message = result.ErrorMessage ?? "Không thể tạo thanh toán MoMo" 
                });
            }

            _logger.LogInformation("User {UserId} tạo MoMo payment cho đơn {OrderId}", 
                userId.Value, request.OrderId);

            return Ok(new { 
                success = true, 
                data = new {
                    paymentUrl = result.PaymentUrl,
                    requestId = result.RequestId,
                    orderId = result.OrderId
                }
            });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi tạo MoMo payment cho đơn {OrderId}", request.OrderId);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    /// <summary>
    /// Xử lý khi user quay về từ MoMo
    /// GET /api/payment/momo/return
    /// </summary>
    [HttpGet("momo/return")]
    [AllowAnonymous]
    public async Task<IActionResult> MomoReturn()
    {
        try
        {
            // Lấy tất cả query params từ MoMo
            var queryParams = Request.Query.ToDictionary(
                kv => kv.Key, 
                kv => kv.Value.ToString()
            );

            if (!queryParams.Any())
            {
                return BadRequest(new { success = false, message = "Không có dữ liệu từ MoMo" });
            }

            // Xử lý callback
            var result = await _momoService.ProcessMoMoReturnAsync(queryParams);

            if (result.Success)
            {
                _logger.LogInformation("MoMo payment thành công cho đơn {OrderId}", result.OrderId);
                
                // Redirect đến trang order success
                return Redirect($"/Order/Tracking/{result.OrderId}?payment=success");
            }
            else
            {
                _logger.LogWarning("MoMo payment thất bại cho đơn {OrderId}. Lỗi: {Error}", 
                    result.OrderId, result.ErrorMessage);
                
                // Redirect đến trang payment failed
                return Redirect($"/Checkout/Payment?orderId={result.OrderId}&error={Uri.EscapeDataString(result.ErrorMessage ?? "Unknown error")}");
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý MoMo return");
            return Redirect("/payment/error");
        }
    }

    /// <summary>
    /// Xử lý IPN từ MoMo
    /// POST /api/payment/momo/ipn
    /// </summary>
    [HttpPost("momo/ipn")]
    [AllowAnonymous]
    public async Task<IActionResult> MomoIPN()
    {
        try
        {
            // MoMo gửi IPN qua POST body
            var postData = new Dictionary<string, string>();
            
            // Đọc từ form data
            foreach (var key in Request.Form.Keys)
            {
                postData[key] = Request.Form[key].ToString();
            }

            // Hoặc từ query string nếu MoMo dùng GET
            if (!postData.Any())
            {
                postData = Request.Query.ToDictionary(
                    kv => kv.Key, 
                    kv => kv.Value.ToString()
                );
            }

            if (!postData.Any())
            {
                return Ok(new { resultCode = 1000, message = "Invalid request" });
            }

            // Xử lý IPN
            var result = await _momoService.ProcessMoMoIPNAsync(postData);

            if (result.Success)
            {
                _logger.LogInformation("MoMo IPN thành công cho đơn {OrderId}", result.OrderId);
                
                // Response theo format MoMo yêu cầu
                return Ok(new { resultCode = 0, message = "Success" });
            }
            else
            {
                _logger.LogWarning("MoMo IPN thất bại: {Message}", result.Message);
                return Ok(new { resultCode = 1000, message = result.Message });
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi xử lý MoMo IPN");
            return Ok(new { resultCode = 1000, message = "System error" });
        }
    }

    /// <summary>
    /// Admin hoàn tiền cho đơn hàng
    /// POST /api/payment/orders/{id}/refund
    /// Body: { "reason": "Lý do hoàn tiền" }
    /// </summary>
    [HttpPost("orders/{id}/refund")]
    [Authorize(Roles = "admin")] // Chỉ admin
    public async Task<IActionResult> RefundPayment(long id, [FromBody] RefundPaymentRequest request)
    {
        try
        {
            var adminId = GetUserId();
            if (adminId == null)
            {
                return Unauthorized(new { success = false, message = "Không xác định được admin" });
            }

            // Validate reason
            if (string.IsNullOrWhiteSpace(request.Reason))
            {
                return BadRequest(new { success = false, message = "Vui lòng nhập lý do hoàn tiền" });
            }

            // Gọi service để xử lý refund
            var result = await _vnpayService.RefundPaymentAsync(id, request.Reason);

            if (!result.Success)
            {
                return BadRequest(new { success = false, message = result.Message });
            }

            _logger.LogInformation("Admin {AdminId} hoàn tiền đơn {OrderId}. Lý do: {Reason}", 
                adminId.Value, id, request.Reason);

            return Ok(new { success = true, message = result.Message, data = result.Data });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Lỗi khi hoàn tiền đơn {OrderId}", id);
            return StatusCode(500, new { success = false, message = "Lỗi server" });
        }
    }

    // ==================== PRIVATE HELPERS ====================

    /// <summary>
    /// Lấy User ID từ JWT claims
    /// </summary>
    private long? GetUserId()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (string.IsNullOrEmpty(userIdClaim) || !long.TryParse(userIdClaim, out var userId))
        {
            return null;
        }
        return userId;
    }
}

/// <summary>
/// Request model để tạo payment
/// </summary>
public class CreatePaymentRequest
{
    public long OrderId { get; set; }
    public string ReturnUrl { get; set; } = string.Empty;
}

/// <summary>
/// Request model để hoàn tiền
/// </summary>
public class RefundPaymentRequest
{
    public string Reason { get; set; } = string.Empty;
}
