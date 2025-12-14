using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;
using System.Text.Json;

namespace WebApp.Services
{
    public interface IAdminProductsService
    {
        object Manage(string? search, int? status, int page, int pageSize, string? sortBy, string? sortDirection);
        List<string> UploadFiles(IFormFileCollection files);
        long Create(ProductManageRequest req);
        bool Update(long id, ProductManageRequest req);
        bool Delete(long id);
        bool SetStatus(long id, int status);
        string ResolveImageUrl(string img);
    }

    public class AdminProductsService : IAdminProductsService
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public AdminProductsService(IWebHostEnvironment env, IConfiguration config)
        {
            _env = env;
            _config = config;
        }

        // ============================================================
        // Lấy danh sách sản phẩm theo bộ lọc quản trị
        // ============================================================
        public object Manage(string? search, int? status, int page, int pageSize, string? sortBy, string? sortDirection)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var where = "WHERE 1=1";

            // Lọc theo keyword
            if (!string.IsNullOrWhiteSpace(search))
                where += " AND (p.name ILIKE @q OR p.slug ILIKE @q OR c.name ILIKE @q)";

            // Lọc theo trạng thái
            if (status.HasValue)
                where += " AND p.status=@st";

            // Sắp xếp
            var order = " ORDER BY p.id DESC";
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                var dir = sortDirection?.ToLower() == "asc" ? "ASC" : "DESC";
                if (sortBy == "price") order = $" ORDER BY p.price {dir}";
                if (sortBy == "name") order = $" ORDER BY p.name {dir}";
            }

            var offset = Math.Max(0, (page - 1) * pageSize);

            using var cmd = new NpgsqlCommand($@"
                SELECT p.id, p.name, c.name AS category, p.price,
                       COALESCE(p.images->>0,''), p.status
                FROM products p
                JOIN categories c ON p.category_id=c.id
                {where} {order}
                LIMIT @ps OFFSET @off", conn);

            if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@q", $"%{search}%");
            if (status.HasValue) cmd.Parameters.AddWithValue("@st", status.Value);

            cmd.Parameters.AddWithValue("@ps", pageSize);
            cmd.Parameters.AddWithValue("@off", offset);

            var list = new List<object>();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new
                {
                    id = reader.GetInt32(0),
                    name = reader.GetString(1),
                    category = reader.GetString(2),
                    price = reader.GetInt32(3),
                    image = ResolveImageUrl(reader.IsDBNull(4) ? "" : reader.GetString(4)),
                    status = reader.GetInt16(5)
                });
            }

            return new { data = list };
        }

        // ============================================================
        // Upload nhiều file ảnh
        // ============================================================
        public List<string> UploadFiles(IFormFileCollection files)
        {
            var saved = new List<string>();

            if (files == null || files.Count == 0)
                return saved;

            var allowed = new HashSet<string> { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var folder = Path.Combine(_env.WebRootPath, "assets/images/products");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            foreach (var f in files)
            {
                var ext = Path.GetExtension(f.FileName).ToLower();

                // Kiểm tra định dạng
                if (!allowed.Contains(ext))
                    throw new Exception("invalid_extension");

                // Kiểm tra dung lượng
                if (f.Length > 5 * 1024 * 1024)
                    throw new Exception("invalid_size");

                var name = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}{ext}";
                var path = Path.Combine(folder, name);

                using var fs = new FileStream(path, FileMode.Create);
                f.CopyTo(fs);

                saved.Add(name);
            }

            return saved;
        }

        // ============================================================
        // Tạo sản phẩm mới
        // ============================================================
        public long Create(ProductManageRequest req)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var slug = NormalizeSlug(req.Slug ?? req.Name);

            using var cmd = new NpgsqlCommand(@"
                INSERT INTO products(category_id, name, slug, description, price, images, status)
                VALUES(@cid, @n, @s, @d, @p, @imgs, @st)
                RETURNING id", conn);

            cmd.Parameters.AddWithValue("@cid", req.CategoryId);
            cmd.Parameters.AddWithValue("@n", req.Name);
            cmd.Parameters.AddWithValue("@s", slug);
            cmd.Parameters.AddWithValue("@d", req.Description ?? "");
            cmd.Parameters.AddWithValue("@p", req.Price);

            var json = JsonSerializer.Serialize(req.Images ?? new List<string>());
            cmd.Parameters.Add(new NpgsqlParameter("@imgs", NpgsqlDbType.Jsonb) { Value = json });

            cmd.Parameters.AddWithValue("@st", req.Status);

            return Convert.ToInt64(cmd.ExecuteScalar());
        }

        // ============================================================
        // Cập nhật sản phẩm
        // ============================================================
        public bool Update(long id, ProductManageRequest req)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var slug = NormalizeSlug(req.Slug ?? req.Name);

            using var cmd = new NpgsqlCommand(@"
                UPDATE products
                SET category_id=@cid, name=@n, slug=@s,
                    description=@d, price=@p,
                    images=@imgs, status=@st
                WHERE id=@id", conn);

            cmd.Parameters.AddWithValue("@cid", req.CategoryId);
            cmd.Parameters.AddWithValue("@n", req.Name);
            cmd.Parameters.AddWithValue("@s", slug);
            cmd.Parameters.AddWithValue("@d", req.Description ?? "");
            cmd.Parameters.AddWithValue("@p", req.Price);

            var json = JsonSerializer.Serialize(req.Images ?? new List<string>());
            cmd.Parameters.Add(new NpgsqlParameter("@imgs", NpgsqlDbType.Jsonb) { Value = json });

            cmd.Parameters.AddWithValue("@st", req.Status);
            cmd.Parameters.AddWithValue("@id", id);

            return cmd.ExecuteNonQuery() > 0;
        }

        // ============================================================
        // Xóa sản phẩm
        // ============================================================
        public bool Delete(long id)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand("DELETE FROM products WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            return cmd.ExecuteNonQuery() > 0;
        }

        // ============================================================
        // Cập nhật trạng thái sản phẩm
        // ============================================================
        public bool SetStatus(long id, int status)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand("UPDATE products SET status=@st WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@st", status);
            cmd.Parameters.AddWithValue("@id", id);

            return cmd.ExecuteNonQuery() > 0;
        }

        // ============================================================
        // Xử lý đường dẫn ảnh
        // ============================================================
        public string ResolveImageUrl(string img)
        {
            if (string.IsNullOrWhiteSpace(img))
                return "/assets/images/docs/placeholder-img.jpg";

            if (img.StartsWith("http"))
                return img;

            var candidate = $"/assets/images/products/{img}";
            var physical = Path.Combine(_env.WebRootPath, "assets/images/products", img);

            return File.Exists(physical)
                ? candidate
                : "/assets/images/products/product-img-1.jpg";
        }

        // ============================================================
        // Chuẩn hóa slug
        // ============================================================
        private string NormalizeSlug(string input)
        {
            if (string.IsNullOrWhiteSpace(input))
                return "item";

            return input.Trim().ToLower().Replace(" ", "-");
        }
    }

    public class ProductManageRequest
    {
        public long CategoryId { get; set; }
        public string Name { get; set; } = "";
        public string Slug { get; set; } = "";
        public string Description { get; set; } = "";
        public int Price { get; set; }
        public int Status { get; set; } = 1;
        public List<string>? Images { get; set; }
    }
}
