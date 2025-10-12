using WebApp.Models;

namespace WebApp.Data
{
    public static class BannerSeeder
    {
        public static async Task SeedBannersAsync(ApplicationDbContext context)
        {
            if (context.Banners.Any())
            {
                return; // Database has been seeded
            }

            var banners = new List<Banner>
            {
                new Banner
                {
                    Name = "Banner Khuyến mãi Tết 2025",
                    Image = "https://images.unsplash.com/photo-1607082348824-0a96f2a4b9da?w=1920&h=600&fit=crop",
                    Link = "https://example.com/sale",
                    Status = 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-10),
                    UpdatedAt = DateTime.UtcNow.AddDays(-10)
                },
                new Banner
                {
                    Name = "Banner Sản phẩm mới",
                    Image = "https://images.unsplash.com/photo-1542838132-92c53300491e?w=1920&h=600&fit=crop",
                    Link = "https://example.com/new-products",
                    Status = 0,
                    CreatedAt = DateTime.UtcNow.AddDays(-8),
                    UpdatedAt = DateTime.UtcNow.AddDays(-8)
                },
                new Banner
                {
                    Name = "Banner Free Shipping",
                    Image = "https://images.unsplash.com/photo-1558618666-fcd25c85cd64?w=1920&h=600&fit=crop",
                    Link = "https://example.com/free-shipping",
                    Status = 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-5),
                    UpdatedAt = DateTime.UtcNow.AddDays(-5)
                },
                new Banner
                {
                    Name = "Banner Organic Food",
                    Image = "https://images.unsplash.com/photo-1490645935967-10de6ba17061?w=1920&h=600&fit=crop",
                    Link = "https://example.com/organic",
                    Status = 1,
                    CreatedAt = DateTime.UtcNow.AddDays(-3),
                    UpdatedAt = DateTime.UtcNow.AddDays(-3)
                },
                new Banner
                {
                    Name = "Banner Flash Sale",
                    Image = "https://images.unsplash.com/photo-1607083206869-4c7672e72a8a?w=1920&h=600&fit=crop",
                    Link = "https://example.com/flash-sale",
                    Status = 0,
                    CreatedAt = DateTime.UtcNow.AddDays(-1),
                    UpdatedAt = DateTime.UtcNow.AddDays(-1)
                }
            };

            context.Banners.AddRange(banners);
            await context.SaveChangesAsync();
        }
    }
}
