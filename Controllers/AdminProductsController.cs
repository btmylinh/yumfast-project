using Microsoft.AspNetCore.Mvc;
using WebApp.Services;
using Npgsql;
using System.Text.Json;
using NpgsqlTypes;
using WebApp.Data;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/products")] // Giữ nguyên route admin
    public class AdminProductsController : BaseController
    {
        private readonly IWebHostEnvironment _env;
        private readonly IConfiguration _config;

        public AdminProductsController(
            IJsonLocalizationService localizationService,
            IWebHostEnvironment env,
            IConfiguration config
        ) : base(localizationService)
        {
            _env = env;
            _config = config;
        }

        // ============================================================
        // ====================== ADMIN API ============================
        // ============================================================

        // Lấy danh sách sản phẩm quản trị
        // GET /api/products/manage
        [HttpGet("manage")]
        public IActionResult Manage(
            [FromQuery] string? search,
            [FromQuery] int? status,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20,
            [FromQuery] string? sortBy = null,
            [FromQuery] string? sortDirection = null)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var where = "WHERE 1=1";
            if (!string.IsNullOrWhiteSpace(search))
                where += " AND (p.name ILIKE @q OR p.slug ILIKE @q OR c.name ILIKE @q)";
            if (status.HasValue)
                where += " AND p.status=@st";

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
                LIMIT @ps OFFSET @off
            ", conn);

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
                    image = ResolveImage(reader.IsDBNull(4) ? "" : reader.GetString(4)),
                    status = reader.GetInt16(5)
                });
            }

            return Ok(new { data = list });
        }

        // Upload ảnh sản phẩm
        // POST /api/products/upload
        [HttpPost("upload")]
        public IActionResult Upload()
        {
            var files = Request.Form.Files;
            if (files.Count == 0)
                return BadRequest(new { message = "no_files" });

            var allowed = new HashSet<string> { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var folder = Path.Combine(_env.WebRootPath, "assets/images/products");

            if (!Directory.Exists(folder))
                Directory.CreateDirectory(folder);

            var saved = new List<string>();

            foreach (var f in files)
            {
                var ext = Path.GetExtension(f.FileName).ToLower();
                if (!allowed.Contains(ext))
                    return BadRequest(new { message = "invalid_extension" });

                if (f.Length > 5 * 1024 * 1024)
                    return BadRequest(new { message = "invalid_size" });

                var name = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{ext}";
                var path = Path.Combine(folder, name);

                using var fs = new FileStream(path, FileMode.Create);
                f.CopyTo(fs);

                saved.Add(name);
            }

            return Ok(new { files = saved });
        }

        // Tạo sản phẩm
        // POST /api/products
        [HttpPost("")]
        public IActionResult Create([FromBody] ProductManageRequest req)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var slug = NormalizeSlug(req.Slug ?? req.Name);

            using var cmd = new NpgsqlCommand(@"
                INSERT INTO products(category_id,name,slug,description,price,images,status)
                VALUES(@cid,@n,@s,@d,@p,@imgs,@st) RETURNING id", conn);

            cmd.Parameters.AddWithValue("@cid", req.CategoryId);
            cmd.Parameters.AddWithValue("@n", req.Name);
            cmd.Parameters.AddWithValue("@s", slug);
            cmd.Parameters.AddWithValue("@d", req.Description ?? "");
            cmd.Parameters.AddWithValue("@p", req.Price);

            var json = JsonSerializer.Serialize(req.Images ?? new List<string>());
            cmd.Parameters.Add(new NpgsqlParameter("@imgs", NpgsqlDbType.Jsonb) { Value = json });

            cmd.Parameters.AddWithValue("@st", req.Status);

            var id = Convert.ToInt64(cmd.ExecuteScalar());

            return Ok(new { id });
        }

        // Cập nhật sản phẩm
        // PUT /api/products/{id}
        [HttpPut("{id}")]
        public IActionResult Update(long id, [FromBody] ProductManageRequest req)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var slug = NormalizeSlug(req.Slug ?? req.Name);

            using var cmd = new NpgsqlCommand(@"
                UPDATE products SET category_id=@cid, name=@n, slug=@s,
                description=@d, price=@p, images=@imgs, status=@st
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

            var rows = cmd.ExecuteNonQuery();
            if (rows == 0)
                return NotFound(new { message = "not_found" });

            return Ok(new { id });
        }

        // Xóa sản phẩm
        // DELETE /api/products/{id}
        [HttpDelete("{id}")]
        public IActionResult Delete(long id)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand("DELETE FROM products WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            if (rows == 0)
                return NotFound(new { message = "not_found" });

            return Ok(new { code = "deleted" });
        }

        // Đổi trạng thái
        // PATCH /api/products/{id}/status
        [HttpPatch("{id}/status")]
        public IActionResult SetStatus(long id, [FromBody] int status)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand("UPDATE products SET status=@st WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@st", status);
            cmd.Parameters.AddWithValue("@id", id);

            var rows = cmd.ExecuteNonQuery();
            if (rows == 0)
                return NotFound(new { message = "not_found" });

            return Ok(new { code = "status_updated" });
        }


        // ==================== HÀM PHỤ TRỢ ====================

        private string ResolveImage(string img)
        {
            if (string.IsNullOrWhiteSpace(img))
                return "/assets/images/docs/placeholder-img.jpg";

            if (img.StartsWith("http"))
                return img;

            var candidate = $"/assets/images/products/{img}";
            var physical = Path.Combine(_env.WebRootPath, "assets/images/products", img);

            return System.IO.File.Exists(physical)
                ? candidate
                : "/assets/images/products/product-img-1.jpg";
        }

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
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Price { get; set; }
        public int Status { get; set; } = 1;
        public List<string>? Images { get; set; }
    }
}
