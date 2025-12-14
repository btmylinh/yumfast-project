using Npgsql;

namespace WebApp.Services
{
    public interface IInventoryService
    {
        Task<bool> CheckStockAsync(long productId, int quantity);
        Task<int> GetAvailableStockAsync(long productId);
        Task<bool> ReserveStockAsync(long productId, int quantity);
        Task<bool> ReleaseStockAsync(long productId, int quantity);
        Task<bool> ConfirmStockAsync(long productId, int quantity);
        Task<int> ReleaseExpiredReservationsAsync(int timeoutMinutes = 15); // 🆕
    }

    public class InventoryService : IInventoryService
    {
        private readonly IConfiguration _config;

        public InventoryService(IConfiguration config)
        {
            _config = config;
        }

        /// <summary>
        /// Kiểm tra xem có đủ hàng không
        /// </summary>
        public async Task<bool> CheckStockAsync(long productId, int quantity)
        {
            var available = await GetAvailableStockAsync(productId);
            return available >= quantity;
        }

        /// <summary>
        /// Lấy số lượng hàng available (stock - reserved)
        /// </summary>
        public async Task<int> GetAvailableStockAsync(long productId)
        {
            await using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                SELECT COALESCE(stock_quantity - reserved_quantity, 0)
                FROM inventory
                WHERE product_id = @pid", conn);
            
            cmd.Parameters.AddWithValue("@pid", productId);

            var result = await cmd.ExecuteScalarAsync();
            return result != null && result != DBNull.Value ? Convert.ToInt32(result) : 0;
        }

        /// <summary>
        /// Reserve stock khi add to cart hoặc checkout (chưa confirm)
        /// </summary>
        public async Task<bool> ReserveStockAsync(long productId, int quantity)
        {
            await using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            // Kiểm tra available trước
            await using var checkCmd = new NpgsqlCommand(@"
                SELECT (stock_quantity - reserved_quantity) >= @qty
                FROM inventory
                WHERE product_id = @pid", conn);
            
            checkCmd.Parameters.AddWithValue("@pid", productId);
            checkCmd.Parameters.AddWithValue("@qty", quantity);

            var canReserve = await checkCmd.ExecuteScalarAsync();
            if (canReserve == null || !(bool)canReserve)
            {
                return false; // Không đủ hàng
            }

            // Reserve stock
            await using var cmd = new NpgsqlCommand(@"
                UPDATE inventory
                SET reserved_quantity = reserved_quantity + @qty,
                    updated_at = NOW()
                WHERE product_id = @pid", conn);
            
            cmd.Parameters.AddWithValue("@pid", productId);
            cmd.Parameters.AddWithValue("@qty", quantity);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        /// <summary>
        /// Release reserved stock (khi hủy đơn hoặc timeout cart)
        /// </summary>
        public async Task<bool> ReleaseStockAsync(long productId, int quantity)
        {
            await using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                UPDATE inventory
                SET reserved_quantity = GREATEST(0, reserved_quantity - @qty),
                    updated_at = NOW()
                WHERE product_id = @pid", conn);
            
            cmd.Parameters.AddWithValue("@pid", productId);
            cmd.Parameters.AddWithValue("@qty", quantity);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        /// <summary>
        /// Confirm stock (chuyển từ reserved sang sold - trừ cả stock và reserved)
        /// </summary>
        public async Task<bool> ConfirmStockAsync(long productId, int quantity)
        {
            await using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            await using var cmd = new NpgsqlCommand(@"
                UPDATE inventory
                SET stock_quantity = stock_quantity - @qty,
                    reserved_quantity = GREATEST(0, reserved_quantity - @qty),
                    updated_at = NOW()
                WHERE product_id = @pid
                  AND stock_quantity >= @qty", conn);
            
            cmd.Parameters.AddWithValue("@pid", productId);
            cmd.Parameters.AddWithValue("@qty", quantity);

            var rows = await cmd.ExecuteNonQueryAsync();
            return rows > 0;
        }

        /// <summary>
        /// Release expired reservations (background job sẽ gọi định kỳ)
        /// TODO: Cần table `cart_reservations` để track reservation timestamps
        /// </summary>
        public async Task<int> ReleaseExpiredReservationsAsync(int timeoutMinutes = 15)
        {
            await using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            // Giả sử có bảng cart_reservations với created_at
            // Hiện tại chỉ là placeholder - cần implement đầy đủ
            await using var cmd = new NpgsqlCommand(@"
                -- TODO: Implement with cart_reservations table
                -- DELETE FROM cart_reservations WHERE created_at < NOW() - INTERVAL '@mins minutes'
                -- RETURNING product_id, quantity
                SELECT 0", conn);
            
            cmd.Parameters.AddWithValue("@mins", timeoutMinutes);
            
            // Placeholder return
            return 0;
        }
    }
}
