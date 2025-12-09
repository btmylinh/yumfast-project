using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;
using System;

namespace WebApp.Data
{
    public static class ProductSeeder
    {
        public static async Task SeedProductsAsync(ApplicationDbContext context, IConfiguration config)
        {
            await context.Database.EnsureCreatedAsync();

            var connString = config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(connString)) return;

            await using var conn = new NpgsqlConnection(connString);
            await conn.OpenAsync();

            await using (var checkCmd = new NpgsqlCommand("SELECT COUNT(*) FROM coupons", conn))
            {
                var countObj = await checkCmd.ExecuteScalarAsync();
                var count = (countObj is long l) ? l : Convert.ToInt64(countObj ?? 0);
                if (count > 0) return;
            }

            var sql = @"WITH t AS (
  SELECT NOW() AS nowt
)
INSERT INTO coupons (product_id, code, name, type, value, start_at, end_at, description, total, used_count, status)
VALUES
(NULL,'WELCOME10','Giảm 10% đơn đầu',1,10, (SELECT nowt - INTERVAL '180 days' FROM t),(SELECT nowt + INTERVAL '180 days' FROM t),'Áp cho toàn bộ đơn, tối đa 50k',1000,0,1),
(NULL,'FREESHIP20K','Giảm 20k phí ship',2,20000,(SELECT nowt - INTERVAL '60 days' FROM t),(SELECT nowt + INTERVAL '120 days' FROM t),'Giảm phí vận chuyển',500,0,1),
((SELECT id FROM products WHERE slug='burger-bo-pho-mai'),'BO10K','Burger bò -10k',2,10000,(SELECT nowt - INTERVAL '30 days' FROM t),(SELECT nowt + INTERVAL '90 days' FROM t),'Giảm trực tiếp 10k cho Burger Bò Phô Mai',300,0,1),
((SELECT id FROM products WHERE slug='ga-cay-han-quoc-2'),'GA15','Gà cay -15%',1,15,(SELECT nowt - INTERVAL '15 days' FROM t),(SELECT nowt + INTERVAL '60 days' FROM t),'Áp riêng Gà cay Hàn',200,0,1),
(NULL,'LUNCH30','Trưa vui -30%',1,30,(SELECT date_trunc('day', nowt) + INTERVAL '11 hours' FROM t),(SELECT nowt + INTERVAL '120 days' FROM t),'Khung giờ 11h-14h, tối đa 40k',1000,0,1),
(NULL,'DRINK5K','Nước ngọt -5k',2,5000,(SELECT nowt - INTERVAL '7 days' FROM t),(SELECT nowt + INTERVAL '90 days' FROM t),'Áp các đồ uống',800,0,1),
((SELECT id FROM products WHERE slug='mi-y-bo-bam'),'PASTA7','Mì Ý -7%',1,7,(SELECT nowt - INTERVAL '90 days' FROM t),(SELECT nowt + INTERVAL '90 days' FROM t),'Giảm cho dòng pasta',400,0,1),
((SELECT id FROM products WHERE slug='com-ga-nuoc-mam'),'COM5K','Cơm gà -5k',2,5000,(SELECT nowt - INTERVAL '30 days' FROM t),(SELECT nowt + INTERVAL '120 days' FROM t),'Áp riêng cơm gà nước mắm',300,0,1),
(NULL,'SWEET15','Tráng miệng -15%',1,15,(SELECT nowt - INTERVAL '10 days' FROM t),(SELECT nowt + INTERVAL '100 days' FROM t),'Áp cho tráng miệng',600,0,1),
(NULL,'COMBO20','Combo -20%',1,20,(SELECT nowt - INTERVAL '180 days' FROM t),(SELECT nowt + INTERVAL '180 days' FROM t),'Áp các combo',1000,0,1),
((SELECT id FROM products WHERE slug='tra-sua-tran-chau'),'MILKTEA6K','Trà sữa -6k',2,6000,(SELECT nowt - INTERVAL '5 days' FROM t),(SELECT nowt + INTERVAL '80 days' FROM t),'Áp riêng trà sữa',500,0,1),
((SELECT id FROM products WHERE slug='pepsi-500'),'PEPSI3K','Pepsi -3k',2,3000,(SELECT nowt - INTERVAL '5 days' FROM t),(SELECT nowt + INTERVAL '80 days' FROM t),'Áp riêng Pepsi',500,0,1)
ON CONFLICT (code) DO NOTHING;";

            await using var seedCmd = new NpgsqlCommand(sql, conn);
            await seedCmd.ExecuteNonQueryAsync();
        }
    }
}
