using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;

namespace WebApp.Data
{
    public static class ProductSeeder
    {
        public static Task SeedProductsAsync(ApplicationDbContext context, IConfiguration config)
        {
            // No-op seeder to satisfy development builds. Implement actual seeding if needed.
            return Task.CompletedTask;
        }
    }
}
