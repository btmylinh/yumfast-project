using WebApp.Models;
using Npgsql;
using Microsoft.Extensions.Configuration;

namespace WebApp.Services
{
    public interface IProductCatalogService
    {
        List<ProductItem> GetAll();
        List<string> GetCategories();
        List<ProductItem> Filter(string? category, decimal? min, decimal? max, string? sort);
    }

    public class ProductCatalogService : IProductCatalogService
    {
        private readonly List<ProductItem> _items = new();

        private readonly List<string> _categories = new();

        public ProductCatalogService(IConfiguration config)
        {
            try
            {
                using var conn = new NpgsqlConnection(config.GetConnectionString("DefaultConnection"));
                conn.Open();
                // products schema: products(category_id, name, images JSONB, price INT, status) + categories(name)
                using var cmd = new NpgsqlCommand(
                    "SELECT p.id, p.name, c.name AS category, p.price, COALESCE(p.images->>0, '') AS image " +
                    "FROM products p JOIN categories c ON p.category_id=c.id " +
                    "WHERE p.status = 1 ORDER BY p.created_at DESC",
                    conn
                );
                using var reader = cmd.ExecuteReader();
                var loaded = new List<ProductItem>();
                while (reader.Read())
                {
                    var img = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                    // map relative filenames to assets path if needed
                    var imagePath = string.IsNullOrWhiteSpace(img) ? "/assets/images/docs/placeholder-img.jpg" : (img.StartsWith("/") ? img : $"/assets/images/products/{img}");
                    loaded.Add(new ProductItem
                    {
                        Id = reader.GetInt32(0),
                        Name = reader.GetString(1),
                        Category = reader.GetString(2),
                        Price = Convert.ToDecimal(reader.GetInt32(3)),
                        Image = imagePath
                    });
                }
                if (loaded.Count > 0)
                {
                    _items.Clear();
                    _items.AddRange(loaded);
                }

                // load categories
                using var catCmd = new NpgsqlCommand("SELECT name FROM categories WHERE status=1 ORDER BY name", conn);
                using var catReader = catCmd.ExecuteReader();
                var cats = new List<string>();
                while (catReader.Read())
                {
                    cats.Add(catReader.GetString(0));
                }
                if (cats.Count > 0)
                {
                    _categories.Clear();
                    _categories.AddRange(cats);
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[Catalog ERROR] {ex.Message}");
                _items.Clear();
            }
        }

        public List<ProductItem> GetAll() => _items.ToList();

        public List<string> GetCategories() => _categories.Count > 0 ? _categories : _items.Select(i => i.Category).Distinct().OrderBy(c => c).ToList();

        public List<ProductItem> Filter(string? category, decimal? min, decimal? max, string? sort)
        {
            var q = _items.AsQueryable();
            if (!string.IsNullOrWhiteSpace(category)) q = q.Where(i => i.Category.Equals(category, StringComparison.OrdinalIgnoreCase));
            if (min.HasValue) q = q.Where(i => i.Price >= min.Value);
            if (max.HasValue) q = q.Where(i => i.Price <= max.Value);
            if (!string.IsNullOrWhiteSpace(sort))
            {
                if (sort == "price_asc") q = q.OrderBy(i => i.Price);
                else if (sort == "price_desc") q = q.OrderByDescending(i => i.Price);
            }
            return q.ToList();
        }
    }
}
