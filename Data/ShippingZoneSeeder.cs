using Npgsql;

namespace WebApp.Data
{
    public class ShippingZoneSeeder
    {
        public static void Seed(string connectionString)
        {
            using var conn = new NpgsqlConnection(connectionString);
            conn.Open();

            // Tạo bảng shipping_zones nếu chưa có
            using (var cmd = new NpgsqlCommand(@"
                CREATE TABLE IF NOT EXISTS shipping_zones (
                    id BIGSERIAL PRIMARY KEY,
                    code VARCHAR(50) UNIQUE NOT NULL,
                    name VARCHAR(255) NOT NULL,
                    base_fee INTEGER NOT NULL DEFAULT 0,
                    free_minimum INTEGER NULL,
                    status SMALLINT NOT NULL DEFAULT 1,
                    created_at TIMESTAMP NOT NULL DEFAULT NOW(),
                    updated_at TIMESTAMP NOT NULL DEFAULT NOW()
                )", conn))
            {
                cmd.ExecuteNonQuery();
            }

            // Kiểm tra xem đã có data chưa
            using (var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM shipping_zones", conn))
            {
                var count = (long)(checkCmd.ExecuteScalar() ?? 0L);
                if (count > 0)
                {
                    Console.WriteLine("✅ Shipping zones already seeded.");
                    return;
                }
            }

            // Seed shipping zones
            var zones = new[]
            {
                new { Code = "inner_city", Name = "Nội thành Hà Nội", BaseFee = 15000, FreeMin = 500000 },
                new { Code = "outer_city", Name = "Ngoại thành Hà Nội", BaseFee = 30000, FreeMin = 800000 },
                new { Code = "north_region", Name = "Miền Bắc (trừ HN)", BaseFee = 45000, FreeMin = 1000000 },
                new { Code = "central_region", Name = "Miền Trung", BaseFee = 60000, FreeMin = 1200000 },
                new { Code = "south_region", Name = "Miền Nam", BaseFee = 75000, FreeMin = 1500000 }
            };

            foreach (var zone in zones)
            {
                using var cmd = new NpgsqlCommand(@"
                    INSERT INTO shipping_zones (code, name, base_fee, free_minimum, status)
                    VALUES (@code, @name, @fee, @min, 1)", conn);
                
                cmd.Parameters.AddWithValue("@code", zone.Code);
                cmd.Parameters.AddWithValue("@name", zone.Name);
                cmd.Parameters.AddWithValue("@fee", zone.BaseFee);
                cmd.Parameters.AddWithValue("@min", (object?)zone.FreeMin ?? DBNull.Value);
                
                cmd.ExecuteNonQuery();
                Console.WriteLine($"✅ Seeded shipping zone: {zone.Name} - {zone.BaseFee:N0}đ");
            }

            Console.WriteLine("✅ Shipping zones seeded successfully!");
        }
    }
}
