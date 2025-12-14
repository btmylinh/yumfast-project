using Npgsql;

namespace WebApp.Data
{
    public class InventorySeeder
    {
        public static void Seed(string connectionString)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            // Tạo bảng inventory nếu chưa có
            using (var cmd = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS inventory (
                    id BIGSERIAL PRIMARY KEY,
                    product_id BIGINT NOT NULL,
                    stock_quantity INTEGER NOT NULL DEFAULT 0,
                    reserved_quantity INTEGER NOT NULL DEFAULT 0,
                    available_quantity INTEGER GENERATED ALWAYS AS (stock_quantity - reserved_quantity) STORED,
                    low_stock_threshold INTEGER NOT NULL DEFAULT 10,
                    updated_at TIMESTAMP NOT NULL DEFAULT NOW(),
                    FOREIGN KEY (product_id) REFERENCES products(id) ON DELETE CASCADE,
                    UNIQUE(product_id)
                )", conn))
            {
                cmd.ExecuteNonQuery();
            }

            // Kiểm tra xem đã có data chưa
            using (var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM inventory", conn))
            {
                var count = (long)(checkCmd.ExecuteScalar() ?? 0L);
                if (count > 0)
                {
                    Console.WriteLine("✅ Inventory already seeded.");
                    return;
                }
            }

            // Lấy danh sách products
            var products = new List<long>();
            using (var cmd = new NpgsqlCommand("SELECT id FROM products LIMIT 100", conn))
            using (var reader = cmd.ExecuteReader())
            {
                while (reader.Read())
                {
                    products.Add(reader.GetInt64(0));
                }
            }

            if (products.Count == 0)
            {
                Console.WriteLine("⚠️  No products found. Please seed products first.");
                return;
            }

            // Seed inventory cho mỗi sản phẩm
            var random = new Random();
            foreach (var productId in products)
            {
                var stockQty = random.Next(20, 200); // Random từ 20-200
                var reservedQty = random.Next(0, Math.Min(10, stockQty)); // Reserved tối đa 10 hoặc < stock
                
                using var cmd = new NpgsqlCommand(@"
                    INSERT INTO inventory (product_id, stock_quantity, reserved_quantity, low_stock_threshold)
                    VALUES (@pid, @stock, @reserved, @threshold)
                    ON CONFLICT (product_id) DO UPDATE 
                    SET stock_quantity = @stock, reserved_quantity = @reserved", conn);
                
                cmd.Parameters.AddWithValue("@pid", productId);
                cmd.Parameters.AddWithValue("@stock", stockQty);
                cmd.Parameters.AddWithValue("@reserved", reservedQty);
                cmd.Parameters.AddWithValue("@threshold", 10);
                
                cmd.ExecuteNonQuery();
            }

            Console.WriteLine($"✅ Inventory seeded for {products.Count} products!");
        }
    }
}
