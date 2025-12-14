using System.Threading.Tasks;

namespace WebApp.Data
{
    public static class BannerSeeder
    {
        public static Task SeedBannersAsync(ApplicationDbContext context)
        {
            // No-op seeder to satisfy development builds. Implement actual seeding if needed.
            return Task.CompletedTask;
        }
    }
}

