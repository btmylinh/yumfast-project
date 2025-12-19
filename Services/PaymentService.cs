using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.SignalR;
using Npgsql;
using WebApp.Hubs;

namespace WebApp.Services
{
    public interface IPaymentService
    {
        Task<object> CreatePaymentAsync(long orderId, string method);
        Task<object> HandleCallbackAsync(long orderId, bool success, string method);
    }

    public class PaymentService : IPaymentService
    {
        private readonly IConfiguration _config;
        private readonly IHubContext<OrderHub> _hubContext;

        public PaymentService(IConfiguration config, IHubContext<OrderHub> hubContext)
        {
            _config = config;
            _hubContext = hubContext;
        }

        // ---------------------------------------------------------
        // Tạo payment pending
        // ---------------------------------------------------------
        public async Task<object> CreatePaymentAsync(long id, string method)
        {
            method = method.ToUpperInvariant();

            await using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            // Kiểm tra đơn hàng tồn tại
            int amount = 0;
            await using (var chk = new NpgsqlCommand("SELECT total_price FROM orders WHERE id=@id", conn))
            {
                chk.Parameters.AddWithValue("@id", id);

                await using var r = await chk.ExecuteReaderAsync();
                if (!await r.ReadAsync())
                    return new { message = "order_not_found" };

                amount = r.GetInt32(0);
                await r.DisposeAsync();
            }

            // Tạo payment
            await using var ins = new NpgsqlCommand(@"
                INSERT INTO order_payments(order_id, amount, status, method)
                VALUES (@oid, @amount, 0, @method)", conn);

            ins.Parameters.AddWithValue("@oid", id);
            ins.Parameters.AddWithValue("@amount", amount);
            ins.Parameters.AddWithValue("@method", method);

            await ins.ExecuteNonQueryAsync();

            string? url = method switch
            {
                "VNPAY" => $"https://sandbox.vnpayment.vn/paymentv2/vpcpay.html?orderId={id}",
                "MOMO" => $"https://test-payment.momo.vn/pay?orderId={id}",
                _ => null
            };

            return new { status = "pending", paymentUrl = url };
        }

        // ---------------------------------------------------------
        // Xử lý callback thanh toán
        // ---------------------------------------------------------
        public async Task<object> HandleCallbackAsync(long orderId, bool success, string method)
        {
            await using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                short newStatus = (short)(success ? 1 : 2);

                // Cập nhật record thanh toán
                await using (var up = new NpgsqlCommand(@"
                    UPDATE order_payments 
                       SET status=@st, paid_at=CASE WHEN @st=1 THEN NOW() ELSE paid_at END
                     WHERE order_id=@oid AND method=@m",
                    conn, (NpgsqlTransaction)tx))
                {
                    up.Parameters.AddWithValue("@st", newStatus);
                    up.Parameters.AddWithValue("@oid", orderId);
                    up.Parameters.AddWithValue("@m", method);
                    await up.ExecuteNonQueryAsync();
                }

                // Nếu trả thành công -> cập nhật đơn hàng
                if (success)
                {
                    await using (var oup = new NpgsqlCommand(@"
                        UPDATE orders 
                           SET payment_status=1, status=1, updated_at=NOW() 
                         WHERE id=@id",
                        conn, (NpgsqlTransaction)tx))
                    {
                        oup.Parameters.AddWithValue("@id", orderId);
                        await oup.ExecuteNonQueryAsync();
                    }

                    // Ghi lịch sử
                    await using (var hcmd = new NpgsqlCommand(@"
                        INSERT INTO order_status_history(order_id, status, note, created_by)
                        VALUES (@oid, 1, 'Payment completed - Waiting for driver', NULL)",
                        conn, (NpgsqlTransaction)tx))
                    {
                        hcmd.Parameters.AddWithValue("@oid", orderId);
                        await hcmd.ExecuteNonQueryAsync();
                    }
                }

                await tx.CommitAsync();
                
                // 🔔 Gửi SignalR notification đến tất cả driver khi có đơn mới (status = 1)
                if (success)
                {
                    try
                    {
                        await _hubContext.Clients.All.SendAsync("NewOrderAvailable", new
                        {
                            orderId,
                            message = "Có đơn hàng mới cần tài xế",
                            timestamp = DateTime.UtcNow
                        });
                    }
                    catch
                    {
                        // Log lỗi nhưng không throw (để không ảnh hưởng logic chính)
                    }
                }
                
                return new { orderId, paid = success };
            }
            catch (PostgresException ex)
            {
                await tx.RollbackAsync();
                return new { error = "db_error", detail = ex.MessageText };
            }
        }
    }
}
