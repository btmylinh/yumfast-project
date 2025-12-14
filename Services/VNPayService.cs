using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Npgsql;
using WebApp.Models;
using WebApp.Services.Interfaces; // For ServiceResult helper class

namespace WebApp.Services;

/// <summary>
/// VNPay Payment Service Implementation
/// TODO: Implement IPaymentService interface after refactoring
/// </summary>
public class VNPayService
{
    private readonly NpgsqlConnection _connection;
    private readonly ILogger<VNPayService> _logger;
    private readonly IConfiguration _configuration;
    
    // VNPay Config
    private string VnpTmnCode => _configuration["Payment:VNPay:TmnCode"] ?? "";
    private string VnpHashSecret => _configuration["Payment:VNPay:HashSecret"] ?? "";
    private string VnpUrl => _configuration["Payment:VNPay:Url"] ?? "";
    private string VnpVersion => "2.1.0";
    private string VnpCommand => "pay";
    
    public VNPayService(
        NpgsqlConnection connection, 
        ILogger<VNPayService> logger,
        IConfiguration configuration)
    {
        _connection = connection;
        _logger = logger;
        _configuration = configuration;
    }
    
    #region VNPay Implementation
    
    public async Task<PaymentResponseViewModel> CreateVNPayPaymentAsync(long orderId, string returnUrl, string ipAddress)
    {
        try
        {
            // Get order info
            var order = await GetOrderAsync(orderId);
            if (order == null)
                return new PaymentResponseViewModel { Success = false, ErrorMessage = "Order not found" };
            
            // Create payment transaction record
            var transactionId = $"VNP{orderId}{DateTime.Now:yyyyMMddHHmmss}";
            await SavePaymentTransactionAsync(orderId, "vnpay", transactionId, order.TotalPrice, "pending", null, null);
            
            // Build VNPay URL
            var vnpParams = new SortedDictionary<string, string>
            {
                { "vnp_Version", VnpVersion },
                { "vnp_Command", VnpCommand },
                { "vnp_TmnCode", VnpTmnCode },
                { "vnp_Amount", (order.TotalPrice * 100).ToString() }, // VNPay yêu cầu số tiền x100
                { "vnp_CreateDate", DateTime.Now.ToString("yyyyMMddHHmmss") },
                { "vnp_CurrCode", "VND" },
                { "vnp_IpAddr", ipAddress },
                { "vnp_Locale", "vn" },
                { "vnp_OrderInfo", $"Thanh toan don hang {order.OrderCode}" },
                { "vnp_OrderType", "other" },
                { "vnp_ReturnUrl", returnUrl },
                { "vnp_TxnRef", transactionId }
            };
            
            // Generate secure hash
            var signData = string.Join("&", vnpParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
            var vnpSecureHash = HmacSHA512(VnpHashSecret, signData);
            
            // Build final URL
            var paymentUrl = $"{VnpUrl}?{signData}&vnp_SecureHash={vnpSecureHash}";
            
            // Save request data
            await UpdatePaymentTransactionRequestAsync(transactionId, vnpParams.ToDictionary(kv => kv.Key, kv => kv.Value));
            
            _logger.LogInformation("Created VNPay payment URL for order {OrderId}", orderId);
            
            return new PaymentResponseViewModel
            {
                Success = true,
                PaymentUrl = paymentUrl,
                TransactionId = transactionId
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating VNPay payment for order {OrderId}", orderId);
            return new PaymentResponseViewModel { Success = false, ErrorMessage = "Failed to create payment" };
        }
    }
    
    public async Task<PaymentCallbackViewModel> ProcessVNPayReturnAsync(Dictionary<string, string> queryParams)
    {
        try
        {
            // Validate signature
            if (!ValidateVNPaySignature(queryParams))
            {
                _logger.LogWarning("Invalid VNPay signature");
                return new PaymentCallbackViewModel
                {
                    Success = false,
                    ErrorMessage = "Invalid signature"
                };
            }
            
            var txnRef = queryParams.GetValueOrDefault("vnp_TxnRef", "");
            var responseCode = queryParams.GetValueOrDefault("vnp_ResponseCode", "");
            var transactionNo = queryParams.GetValueOrDefault("vnp_TransactionNo", "");
            var amount = long.Parse(queryParams.GetValueOrDefault("vnp_Amount", "0")) / 100;
            
            // Save response data
            await UpdatePaymentTransactionResponseAsync(txnRef, queryParams);
            
            if (responseCode == "00") // Success
            {
                var orderId = ExtractOrderIdFromTxnRef(txnRef);
                await ProcessPaymentSuccessAsync(orderId, transactionNo, "vnpay");
                
                return new PaymentCallbackViewModel
                {
                    Success = true,
                    OrderId = orderId,
                    TransactionId = transactionNo,
                    Amount = (int)amount,
                    PaidAt = DateTime.Now
                };
            }
            else
            {
                var orderId = ExtractOrderIdFromTxnRef(txnRef);
                var errorMessage = GetVNPayErrorMessage(responseCode);
                await ProcessPaymentFailureAsync(orderId, errorMessage);
                
                return new PaymentCallbackViewModel
                {
                    Success = false,
                    OrderId = orderId,
                    ErrorMessage = errorMessage,
                    ResponseCode = responseCode
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing VNPay return");
            return new PaymentCallbackViewModel { Success = false, ErrorMessage = "Processing error" };
        }
    }
    
    public async Task<PaymentCallbackViewModel> ProcessVNPayIPNAsync(Dictionary<string, string> queryParams)
    {
        // IPN xử lý tương tự Return nhưng không redirect
        return await ProcessVNPayReturnAsync(queryParams);
    }
    
    #endregion
    
    #region Momo Implementation (Stub - cần implement đầy đủ)
    
    public async Task<PaymentResponseViewModel> CreateMomoPaymentAsync(long orderId, string returnUrl, string ipAddress)
    {
        // TODO: Implement Momo payment
        _logger.LogWarning("Momo payment not implemented yet");
        await Task.CompletedTask;
        return new PaymentResponseViewModel { Success = false, ErrorMessage = "Momo payment not implemented" };
    }
    
    public async Task<PaymentCallbackViewModel> ProcessMomoReturnAsync(Dictionary<string, string> postData)
    {
        // TODO: Implement Momo return processing
        await Task.CompletedTask;
        return new PaymentCallbackViewModel { Success = false, ErrorMessage = "Not implemented" };
    }
    
    public async Task<PaymentCallbackViewModel> ProcessMomoIPNAsync(Dictionary<string, string> postData)
    {
        // TODO: Implement Momo IPN
        await Task.CompletedTask;
        return new PaymentCallbackViewModel { Success = false, ErrorMessage = "Not implemented" };
    }
    
    #endregion
    
    #region Common Methods
    
    public async Task<bool> ProcessPaymentSuccessAsync(long orderId, string transactionId, string paymentMethod)
    {
        await using var transaction = await _connection.BeginTransactionAsync();
        
        try
        {
            // Update order payment status
            var updateOrderSql = @"
                UPDATE orders 
                SET payment_status = 1, updated_at = NOW()
                WHERE id = @orderId";
            
            await _connection.ExecuteAsync(updateOrderSql, new { orderId }, transaction);
            
            // Update payment transaction
            var updateTxnSql = @"
                UPDATE payment_transactions 
                SET status = 'success', paid_at = NOW(), updated_at = NOW()
                WHERE transaction_id = @transactionId";
            
            await _connection.ExecuteAsync(updateTxnSql, new { transactionId }, transaction);
            
            // Create order payment record
            var insertPaymentSql = @"
                INSERT INTO order_payments (order_id, amount, method, provider_txn_id, status, paid_at, created_at)
                VALUES (@orderId, (SELECT total_price FROM orders WHERE id = @orderId), @paymentMethod, @transactionId, 1, NOW(), NOW())";
            
            await _connection.ExecuteAsync(insertPaymentSql, new { orderId, paymentMethod, transactionId }, transaction);
            
            // TODO: Confirm inventory (gọi trigger hoặc InventoryService)
            
            await transaction.CommitAsync();
            
            _logger.LogInformation("Payment success for order {OrderId}, transaction {TransactionId}", orderId, transactionId);
            
            return true;
        }
        catch (Exception ex)
        {
            await transaction.RollbackAsync();
            _logger.LogError(ex, "Error processing payment success");
            return false;
        }
    }
    
    public async Task<bool> ProcessPaymentFailureAsync(long orderId, string errorMessage)
    {
        try
        {
            var sql = @"
                UPDATE payment_transactions 
                SET status = 'failed', error_message = @errorMessage, updated_at = NOW()
                WHERE order_id = @orderId AND status = 'pending'";
            
            await _connection.ExecuteAsync(sql, new { orderId, errorMessage });
            
            _logger.LogWarning("Payment failed for order {OrderId}: {ErrorMessage}", orderId, errorMessage);
            
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing payment failure");
            return false;
        }
    }
    
    public async Task<ServiceResult> RefundPaymentAsync(long orderId, string reason)
    {
        // TODO: Implement refund logic
        // 1. Call VNPay/Momo refund API
        // 2. Update payment_transactions status = 'refunded'
        // 3. Update order status = 7 (refunded)
        // 4. Release inventory
        
        _logger.LogWarning("Refund not fully implemented for order {OrderId}", orderId);
        await Task.CompletedTask;
        
        return ServiceResult.Fail("Refund not implemented");
    }
    
    #endregion
    
    #region Helper Methods
    
    private async Task<dynamic?> GetOrderAsync(long orderId)
    {
        var sql = "SELECT id, code as OrderCode, total_price as TotalPrice FROM orders WHERE id = @orderId";
        return await _connection.QueryFirstOrDefaultAsync(sql, new { orderId });
    }
    
    private async Task SavePaymentTransactionAsync(
        long orderId, 
        string paymentMethod, 
        string transactionId, 
        int amount, 
        string status,
        Dictionary<string, string>? requestData,
        Dictionary<string, string>? responseData)
    {
        var sql = @"
            INSERT INTO payment_transactions 
                (order_id, payment_method, transaction_id, amount, status, request_data, response_data, created_at, updated_at)
            VALUES 
                (@orderId, @paymentMethod, @transactionId, @amount, @status, @requestData::jsonb, @responseData::jsonb, NOW(), NOW())
            ON CONFLICT (transaction_id) DO NOTHING";
        
        await _connection.ExecuteAsync(sql, new
        {
            orderId,
            paymentMethod,
            transactionId,
            amount,
            status,
            requestData = requestData != null ? JsonSerializer.Serialize(requestData) : null,
            responseData = responseData != null ? JsonSerializer.Serialize(responseData) : null
        });
    }
    
    private async Task UpdatePaymentTransactionRequestAsync(string transactionId, Dictionary<string, string> requestData)
    {
        var sql = @"
            UPDATE payment_transactions 
            SET request_data = @requestData::jsonb, updated_at = NOW()
            WHERE transaction_id = @transactionId";
        
        await _connection.ExecuteAsync(sql, new
        {
            transactionId,
            requestData = JsonSerializer.Serialize(requestData)
        });
    }
    
    private async Task UpdatePaymentTransactionResponseAsync(string transactionId, Dictionary<string, string> responseData)
    {
        var sql = @"
            UPDATE payment_transactions 
            SET callback_data = @responseData::jsonb, updated_at = NOW()
            WHERE transaction_id = @transactionId";
        
        await _connection.ExecuteAsync(sql, new
        {
            transactionId,
            responseData = JsonSerializer.Serialize(responseData)
        });
    }
    
    private string HmacSHA512(string key, string data)
    {
        var hash = new HMACSHA512(Encoding.UTF8.GetBytes(key));
        var hashBytes = hash.ComputeHash(Encoding.UTF8.GetBytes(data));
        return BitConverter.ToString(hashBytes).Replace("-", "").ToLower();
    }
    
    private bool ValidateVNPaySignature(Dictionary<string, string> queryParams)
    {
        var vnpSecureHash = queryParams.GetValueOrDefault("vnp_SecureHash", "");
        queryParams.Remove("vnp_SecureHash");
        queryParams.Remove("vnp_SecureHashType");
        
        var sortedParams = new SortedDictionary<string, string>(queryParams);
        var signData = string.Join("&", sortedParams.Select(kv => $"{kv.Key}={Uri.EscapeDataString(kv.Value)}"));
        var calculatedHash = HmacSHA512(VnpHashSecret, signData);
        
        return calculatedHash.Equals(vnpSecureHash, StringComparison.InvariantCultureIgnoreCase);
    }
    
    private long ExtractOrderIdFromTxnRef(string txnRef)
    {
        // Format: VNP{orderId}{timestamp}
        // Example: VNP12320241213120000
        var match = System.Text.RegularExpressions.Regex.Match(txnRef, @"VNP(\d+)");
        if (match.Success && long.TryParse(match.Groups[1].Value, out var orderId))
            return orderId;
        return 0;
    }
    
    private string GetVNPayErrorMessage(string responseCode)
    {
        return responseCode switch
        {
            "00" => "Giao dịch thành công",
            "07" => "Trừ tiền thành công. Giao dịch bị nghi ngờ",
            "09" => "Giao dịch không thành công do: Thẻ/Tài khoản chưa đăng ký dịch vụ",
            "10" => "Giao dịch không thành công do: Xác thực thông tin thẻ/tài khoản không đúng quá 3 lần",
            "11" => "Giao dịch không thành công do: Đã hết hạn chờ thanh toán",
            "12" => "Giao dịch không thành công do: Thẻ/Tài khoản bị khóa",
            "13" => "Giao dịch không thành công do Quý khách nhập sai mật khẩu xác thực giao dịch (OTP)",
            "24" => "Giao dịch không thành công do: Khách hàng hủy giao dịch",
            "51" => "Giao dịch không thành công do: Tài khoản không đủ số dư",
            "65" => "Giao dịch không thành công do: Tài khoản đã vượt quá hạn mức giao dịch trong ngày",
            "75" => "Ngân hàng thanh toán đang bảo trì",
            "79" => "Giao dịch không thành công do: KH nhập sai mật khẩu thanh toán quá số lần quy định",
            _ => "Giao dịch thất bại"
        };
    }
    
    #endregion
}
