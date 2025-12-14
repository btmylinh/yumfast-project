using System.Security.Cryptography;
using System.Text;
using Npgsql;

namespace WebApp.Services;

/// <summary>
/// Service giả lập thanh toán MoMo
/// Trong production sẽ tích hợp API thật
/// </summary>
public class MoMoService
{
    private readonly IConfiguration _config;
    private readonly ILogger<MoMoService> _logger;

    public MoMoService(IConfiguration config, ILogger<MoMoService> logger)
    {
        _config = config;
        _logger = logger;
    }

    /// <summary>
    /// Tạo URL thanh toán MoMo (giả lập)
    /// </summary>
    public async Task<MoMoPaymentResult> CreateMoMoPaymentAsync(long orderId, string returnUrl, string notifyUrl)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            // Lấy thông tin đơn hàng
            await using var cmd = new NpgsqlCommand(@"
                SELECT total_price, code
                FROM orders 
                WHERE id = @id", conn);
            cmd.Parameters.AddWithValue("@id", orderId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return new MoMoPaymentResult
                {
                    Success = false,
                    ErrorMessage = "Đơn hàng không tồn tại"
                };
            }

            var amount = reader.GetInt32(0);
            var orderCode = reader.IsDBNull(1) ? $"ORD{orderId}" : reader.GetString(1);
            await reader.CloseAsync();

            // Tạo request ID theo format MoMo (thêm milliseconds để tránh duplicate)
            var requestId = $"MOMO{orderId}{DateTime.Now:yyyyMMddHHmmssfff}";
            var orderId_str = $"MM{orderId}";

            // Lưu payment record vào DB (transaction_id sẽ NULL lúc đầu)
            await using var insertCmd = new NpgsqlCommand(@"
                INSERT INTO payment_transactions 
                (order_id, payment_method, amount, status, request_data)
                VALUES (@orderId, 'MOMO', @amount, 'pending', jsonb_build_object('requestId', @requestId))
                RETURNING id", conn);

            insertCmd.Parameters.AddWithValue("@orderId", orderId);
            insertCmd.Parameters.AddWithValue("@amount", amount);
            insertCmd.Parameters.AddWithValue("@requestId", requestId);

            var paymentId = await insertCmd.ExecuteScalarAsync();

            // Tạo URL giả lập (redirect đến trang giả lập MoMo của chúng ta)
            var baseUrl = _config["AppSettings:BaseUrl"] ?? "http://localhost:5125";
            var paymentUrl = $"{baseUrl}/Payment/MoMoSimulator?orderId={orderId}&amount={amount}&orderInfo={Uri.EscapeDataString($"Thanh toán đơn hàng {orderCode}")}&returnUrl={Uri.EscapeDataString(returnUrl)}&requestId={requestId}";

            _logger.LogInformation("Created MoMo payment for order {OrderId}, request {RequestId}", orderId, requestId);

            return new MoMoPaymentResult
            {
                Success = true,
                PaymentUrl = paymentUrl,
                RequestId = requestId,
                OrderId = orderId_str
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error creating MoMo payment for order {OrderId}", orderId);
            return new MoMoPaymentResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Xử lý callback từ MoMo (giả lập)
    /// </summary>
    public async Task<MoMoReturnResult> ProcessMoMoReturnAsync(Dictionary<string, string> queryParams)
    {
        try
        {
            // Lấy các tham số
            if (!queryParams.TryGetValue("orderId", out var orderIdStr) ||
                !long.TryParse(orderIdStr, out var orderId))
            {
                return new MoMoReturnResult
                {
                    Success = false,
                    ErrorMessage = "Invalid order ID"
                };
            }

            if (!queryParams.TryGetValue("resultCode", out var resultCodeStr) ||
                !int.TryParse(resultCodeStr, out var resultCode))
            {
                resultCode = 1000; // Default to error
            }

            var requestId = queryParams.GetValueOrDefault("requestId", "");
            var message = queryParams.GetValueOrDefault("message", "");

            await using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            // resultCode = 0 là thành công trong MoMo
            if (resultCode == 0)
            {
                // Cập nhật payment transaction (tìm theo order_id và status = pending)
                await using var updateTxnCmd = new NpgsqlCommand(@"
                    UPDATE payment_transactions 
                    SET status = 'success', 
                        transaction_id = @txnId,
                        response_data = jsonb_build_object('code', @code, 'message', @message),
                        paid_at = NOW(),
                        updated_at = NOW()
                    WHERE order_id = @orderId AND status = 'pending'", conn);

                updateTxnCmd.Parameters.AddWithValue("@orderId", orderId);
                updateTxnCmd.Parameters.AddWithValue("@txnId", requestId);
                updateTxnCmd.Parameters.AddWithValue("@code", resultCode.ToString());
                updateTxnCmd.Parameters.AddWithValue("@message", message);
                await updateTxnCmd.ExecuteNonQueryAsync();

                // Cập nhật đơn hàng
                await using var updateOrderCmd = new NpgsqlCommand(@"
                    UPDATE orders 
                    SET payment_status = 1, 
                        status = 2,
                        updated_at = NOW()
                    WHERE id = @id", conn);

                updateOrderCmd.Parameters.AddWithValue("@id", orderId);
                await updateOrderCmd.ExecuteNonQueryAsync();

                _logger.LogInformation("MoMo payment completed for order {OrderId}", orderId);

                return new MoMoReturnResult
                {
                    Success = true,
                    OrderId = orderId,
                    RequestId = requestId,
                    Message = "Thanh toán thành công"
                };
            }
            else
            {
                // Thanh toán thất bại
                await using var updateTxnCmd = new NpgsqlCommand(@"
                    UPDATE payment_transactions 
                    SET status = 'failed', 
                        transaction_id = @txnId,
                        response_data = jsonb_build_object('code', @code, 'message', @message),
                        error_message = @message,
                        updated_at = NOW()
                    WHERE order_id = @orderId AND status = 'pending'", conn);

                updateTxnCmd.Parameters.AddWithValue("@orderId", orderId);
                updateTxnCmd.Parameters.AddWithValue("@txnId", requestId);
                updateTxnCmd.Parameters.AddWithValue("@code", resultCode.ToString());
                updateTxnCmd.Parameters.AddWithValue("@message", message);
                await updateTxnCmd.ExecuteNonQueryAsync();

                return new MoMoReturnResult
                {
                    Success = false,
                    OrderId = orderId,
                    ErrorMessage = message ?? "Payment failed or cancelled"
                };
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error processing MoMo return");
            return new MoMoReturnResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }

    /// <summary>
    /// Xử lý IPN từ MoMo (giả lập)
    /// </summary>
    public async Task<MoMoIPNResult> ProcessMoMoIPNAsync(Dictionary<string, string> postData)
    {
        // Tương tự ProcessMoMoReturnAsync nhưng response format khác
        var returnResult = await ProcessMoMoReturnAsync(postData);
        
        return new MoMoIPNResult
        {
            Success = returnResult.Success,
            OrderId = returnResult.OrderId,
            Message = returnResult.Message ?? returnResult.ErrorMessage ?? "Unknown"
        };
    }

    /// <summary>
    /// Query transaction status (giả lập)
    /// </summary>
    public async Task<MoMoQueryResult> QueryTransactionAsync(long orderId, string requestId)
    {
        try
        {
            await using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                SELECT status, amount, response_data 
                FROM payment_transactions 
                WHERE order_id = @orderId AND (transaction_id = @requestId OR request_data->>'requestId' = @requestId)", conn);
            
            cmd.Parameters.AddWithValue("@orderId", orderId);
            cmd.Parameters.AddWithValue("@requestId", requestId);

            await using var reader = await cmd.ExecuteReaderAsync();
            if (!await reader.ReadAsync())
            {
                return new MoMoQueryResult
                {
                    Success = false,
                    ErrorMessage = "Transaction not found"
                };
            }

            var status = reader.GetString(0);
            var amount = reader.GetInt32(1);

            return new MoMoQueryResult
            {
                Success = true,
                Status = status,
                Amount = amount,
                ResultCode = status == "success" ? 0 : 1000
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error querying MoMo transaction");
            return new MoMoQueryResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

#region Result Models

public class MoMoPaymentResult
{
    public bool Success { get; set; }
    public string? PaymentUrl { get; set; }
    public string? RequestId { get; set; }
    public string? OrderId { get; set; }
    public string? ErrorMessage { get; set; }
}

public class MoMoReturnResult
{
    public bool Success { get; set; }
    public long OrderId { get; set; }
    public string? RequestId { get; set; }
    public string? Message { get; set; }
    public string? ErrorMessage { get; set; }
}

public class MoMoIPNResult
{
    public bool Success { get; set; }
    public long OrderId { get; set; }
    public string? Message { get; set; }
}

public class MoMoQueryResult
{
    public bool Success { get; set; }
    public string? Status { get; set; }
    public int Amount { get; set; }
    public int ResultCode { get; set; }
    public string? ErrorMessage { get; set; }
}

#endregion
