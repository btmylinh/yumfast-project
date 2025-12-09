using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Npgsql;
using System.IO;
using System.Text.Json;
using NpgsqlTypes;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/products")]
    public class ProductsApiController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public ProductsApiController(IConfiguration config, IWebHostEnvironment env)
        {
            _config = config;
            _env = env;
        }

        [HttpGet("manage")]
        public IActionResult Manage([FromQuery] string? search, [FromQuery] int? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? sortBy = null, [FromQuery] string? sortDirection = null)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            var where = "WHERE 1=1";
            if (!string.IsNullOrWhiteSpace(search)) where += " AND (p.name ILIKE @q OR p.slug ILIKE @q OR c.name ILIKE @q)";
            if (status.HasValue) where += " AND p.status=@st";
            var order = " ORDER BY p.id DESC";
            if (!string.IsNullOrWhiteSpace(sortBy))
            {
                var dir = (string.Equals(sortDirection, "asc", StringComparison.OrdinalIgnoreCase)) ? "ASC" : "DESC";
                if (sortBy == "price") order = $" ORDER BY p.price {dir}";
                else if (sortBy == "name") order = $" ORDER BY p.name {dir}";
            }
            var offset = Math.Max(0, (page - 1) * pageSize);
            using var cmd = new NpgsqlCommand($"SELECT p.id, p.name, c.name AS category, p.price, COALESCE(p.images->>0,'') AS image, p.status FROM products p JOIN categories c ON p.category_id=c.id {where}{order} LIMIT @ps OFFSET @off", conn);
            if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@q", $"%{search}%");
            if (status.HasValue) cmd.Parameters.AddWithValue("@st", status.Value);
            cmd.Parameters.AddWithValue("@ps", pageSize);
            cmd.Parameters.AddWithValue("@off", offset);
            var list = new List<object>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var img = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                var path = "/assets/images/docs/placeholder-img.jpg";
                if (!string.IsNullOrWhiteSpace(img))
                {
                    if (img.StartsWith("http", StringComparison.OrdinalIgnoreCase)) path = img; else {
                        var candidate = img.StartsWith("/") ? img : $"/assets/images/products/{img}";
                        var physical = img.StartsWith("/") ? System.IO.Path.Combine(_env.WebRootPath, candidate.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar)) : System.IO.Path.Combine(_env.WebRootPath, "assets", "images", "products", img);
                        path = System.IO.File.Exists(physical) ? candidate : "/assets/images/products/product-img-1.jpg";
                    }
                }
                list.Add(new { id = reader.GetInt32(0), name = reader.GetString(1), category = reader.GetString(2), price = reader.GetInt32(3), image = path, status = reader.GetInt16(5) });
            }
            return Ok(new { data = list });
        }

        [HttpPost("upload")]
        public IActionResult Upload()
        {
            var files = Request.Form.Files;
            if (files == null || files.Count == 0) return BadRequest(new { message = "no_files" });
            var allowed = new HashSet<string>(StringComparer.OrdinalIgnoreCase) { ".jpg", ".jpeg", ".png", ".gif", ".webp" };
            var target = System.IO.Path.Combine(_env.WebRootPath, "assets", "images", "products");
            if (!System.IO.Directory.Exists(target)) System.IO.Directory.CreateDirectory(target);
            var saved = new List<string>();
            foreach (var f in files)
            {
                var ext = System.IO.Path.GetExtension(f.FileName);
                if (string.IsNullOrWhiteSpace(ext) || !allowed.Contains(ext)) return BadRequest(new { message = "invalid_extension" });
                if (f.Length <= 0 || f.Length > 5 * 1024 * 1024) return BadRequest(new { message = "invalid_size" });
                var baseName = NormalizeSlug(System.IO.Path.GetFileNameWithoutExtension(f.FileName));
                var fileName = $"{DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()}_{baseName}{ext.ToLowerInvariant()}";
                var path = System.IO.Path.Combine(target, fileName);
                using (var s = System.IO.File.Create(path)) { f.CopyTo(s); }
                saved.Add(fileName);
            }
            return Ok(new { files = saved });
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

        [HttpPost]
        public IActionResult Create([FromBody] ProductManageRequest req)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            var slug = string.IsNullOrWhiteSpace(req.Slug) ? NormalizeSlug(req.Name) : req.Slug.Trim().ToLowerInvariant();
            using var cmd = new NpgsqlCommand("INSERT INTO products(category_id, name, slug, description, price, images, status) VALUES(@cid,@n,@s,@d,@p,@imgs,@st) RETURNING id", conn);
            cmd.Parameters.AddWithValue("@cid", req.CategoryId);
            cmd.Parameters.AddWithValue("@n", req.Name);
            cmd.Parameters.AddWithValue("@s", slug);
            cmd.Parameters.AddWithValue("@d", (object?)req.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("@p", req.Price);
            var json = JsonSerializer.Serialize(req.Images ?? new List<string>());
            var par = new NpgsqlParameter("@imgs", NpgsqlDbType.Jsonb) { Value = json };
            cmd.Parameters.Add(par);
            cmd.Parameters.AddWithValue("@st", req.Status);
            var id = (long)cmd.ExecuteScalar();
            return Ok(new { id });
        }

        [HttpPut("{id}")]
        public IActionResult Update(long id, [FromBody] ProductManageRequest req)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            var slug = string.IsNullOrWhiteSpace(req.Slug) ? NormalizeSlug(req.Name) : req.Slug.Trim().ToLowerInvariant();
            using var cmd = new NpgsqlCommand("UPDATE products SET category_id=@cid, name=@n, slug=@s, description=@d, price=@p, images=@imgs, status=@st WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@cid", req.CategoryId);
            cmd.Parameters.AddWithValue("@n", req.Name);
            cmd.Parameters.AddWithValue("@s", slug);
            cmd.Parameters.AddWithValue("@d", (object?)req.Description ?? string.Empty);
            cmd.Parameters.AddWithValue("@p", req.Price);
            var json = JsonSerializer.Serialize(req.Images ?? new List<string>());
            var par = new NpgsqlParameter("@imgs", NpgsqlDbType.Jsonb) { Value = json };
            cmd.Parameters.Add(par);
            cmd.Parameters.AddWithValue("@st", req.Status);
            cmd.Parameters.AddWithValue("@id", id);
            var rows = cmd.ExecuteNonQuery();
            if (rows == 0) return NotFound(new { message = "not_found" });
            return Ok(new { id });
        }

        [HttpDelete("{id}")]
        public IActionResult Delete(long id)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            using var cmd = new NpgsqlCommand("DELETE FROM products WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            var rows = cmd.ExecuteNonQuery();
            if (rows == 0) return NotFound(new { message = "not_found" });
            return Ok(new { code = "deleted" });
        }

        [HttpPatch("{id}/status")]
        public IActionResult SetStatus(long id, [FromBody] int status)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            using var cmd = new NpgsqlCommand("UPDATE products SET status=@st WHERE id=@id", conn);
            cmd.Parameters.AddWithValue("@st", status);
            cmd.Parameters.AddWithValue("@id", id);
            var rows = cmd.ExecuteNonQuery();
            if (rows == 0) return NotFound(new { message = "not_found" });
            return Ok(new { code = "status_updated" });
        }

        private static string NormalizeSlug(string s)
        {
            var t = (s ?? string.Empty).Trim().ToLowerInvariant();
            t = t.Normalize(System.Text.NormalizationForm.FormD);
            var chars = new System.Text.StringBuilder();
            foreach (var ch in t)
            {
                var cat = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(ch);
                if (cat != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    if ((ch >= 'a' && ch <= 'z') || (ch >= '0' && ch <= '9')) chars.Append(ch);
                    else if (char.IsWhiteSpace(ch) || ch == '-' || ch == '_') chars.Append('-');
                }
            }
            var res = chars.ToString();
            while (res.Contains("--")) res = res.Replace("--", "-");
            return string.IsNullOrWhiteSpace(res) ? "item" : res.Trim('-');
        }
        [HttpGet]
        public IActionResult Get([FromQuery] string? category, [FromQuery] int? min, [FromQuery] int? max, [FromQuery] string? sort, [FromQuery] string? q, [FromQuery] string? tag)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            var where = "WHERE p.status=1";
            if (!string.IsNullOrWhiteSpace(category)) where += " AND c.name=@cat";
            if (min.HasValue) where += " AND p.price>=@min";
            if (max.HasValue) where += " AND p.price<=@max";
            if (!string.IsNullOrWhiteSpace(q)) where += " AND (p.name ILIKE @q OR p.description ILIKE @q)";
            if (!string.IsNullOrWhiteSpace(tag)) where += " AND EXISTS (SELECT 1 FROM product_options po WHERE po.product_id=p.id AND (po.type ILIKE @tag OR po.name ILIKE @tag))";
            var order = " ORDER BY p.created_at DESC";
            if (!string.IsNullOrWhiteSpace(sort))
            {
                if (sort == "price_asc") order = " ORDER BY p.price ASC";
                else if (sort == "price_desc") order = " ORDER BY p.price DESC";
            }
            using var cmd = new NpgsqlCommand($"SELECT p.id, p.name, c.name AS category, p.price, COALESCE(p.images->>0,'') AS image FROM products p JOIN categories c ON p.category_id=c.id {where}{order}", conn);
            if (!string.IsNullOrWhiteSpace(category)) cmd.Parameters.AddWithValue("@cat", category);
            if (min.HasValue) cmd.Parameters.AddWithValue("@min", min.Value);
            if (max.HasValue) cmd.Parameters.AddWithValue("@max", max.Value);
            if (!string.IsNullOrWhiteSpace(q)) cmd.Parameters.AddWithValue("@q", $"%{q}%");
            if (!string.IsNullOrWhiteSpace(tag)) cmd.Parameters.AddWithValue("@tag", $"%{tag}%");
            var list = new List<ProductDto>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                var img = reader.IsDBNull(4) ? string.Empty : reader.GetString(4);
                var path = "/assets/images/docs/placeholder-img.jpg";
                if (!string.IsNullOrWhiteSpace(img))
                {
                    if (img.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                    {
                        path = img;
                    }
                    else
                    {
                        var candidate = img.StartsWith("/") ? img : $"/assets/images/products/{img}";
                    var physical = img.StartsWith("/") ? System.IO.Path.Combine(_env.WebRootPath, candidate.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar)) : System.IO.Path.Combine(_env.WebRootPath, "assets", "images", "products", img);
                    path = System.IO.File.Exists(physical) ? candidate : "/assets/images/products/product-img-1.jpg";
                }
                }
                list.Add(new ProductDto
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Category = reader.GetString(2),
                    Price = reader.GetInt32(3),
                    Image = path
                });
            }
            return Ok(new { data = list });
        }

        [HttpGet("{id}")]
        public IActionResult GetById(long id)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            using var cmd = new NpgsqlCommand("SELECT p.id, p.name, c.name AS category, p.description, p.price, p.images FROM products p JOIN categories c ON p.category_id=c.id WHERE p.id=@id", conn);
            cmd.Parameters.AddWithValue("@id", id);
            using var reader = cmd.ExecuteReader();
            if (!reader.Read()) return NotFound(new { message = "not_found" });

            var imagesJson = reader.IsDBNull(5) ? "[]" : reader.GetString(5);
            var images = new List<string>();
            try
            {
                var arr = System.Text.Json.JsonSerializer.Deserialize<List<string>>(imagesJson) ?? new List<string>();
                foreach (var img in arr)
                {
                    var path = "/assets/images/docs/placeholder-img.jpg";
                    if (!string.IsNullOrWhiteSpace(img))
                    {
                        if (img.StartsWith("http", StringComparison.OrdinalIgnoreCase))
                        {
                            path = img;
                        }
                        else
                        {
                            var candidate = img.StartsWith("/") ? img : $"/assets/images/products/{img}";
                            var physical = img.StartsWith("/") ? System.IO.Path.Combine(_env.WebRootPath, candidate.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar)) : System.IO.Path.Combine(_env.WebRootPath, "assets", "images", "products", img);
                            path = System.IO.File.Exists(physical) ? candidate : "/assets/images/products/product-img-1.jpg";
                        }
                    }
                    images.Add(path);
                }
            }
            catch
            {
                images.Add("/assets/images/docs/placeholder-img.jpg");
            }

            var dto = new ProductDetailDto
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Category = reader.GetString(2),
                Description = reader.IsDBNull(3) ? string.Empty : reader.GetString(3),
                Price = reader.GetInt32(4),
                Images = images
            };

            // options
            reader.Close();
            using var opt = new NpgsqlCommand("SELECT name, type, price FROM product_options WHERE product_id=@pid ORDER BY type, price", conn);
            opt.Parameters.AddWithValue("@pid", id);
            using var r2 = opt.ExecuteReader();
            var options = new List<ProductOptionDto>();
            while (r2.Read())
            {
                options.Add(new ProductOptionDto
                {
                    Name = r2.GetString(0),
                    Type = r2.IsDBNull(1) ? string.Empty : r2.GetString(1),
                    Price = r2.GetInt32(2)
                });
            }
            dto.Options = options;
            return Ok(new { data = dto });
        }
    }

    public class ProductDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public int Price { get; set; }
        public string Image { get; set; } = string.Empty;
    }

    public class ProductDetailDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Category { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public int Price { get; set; }
        public List<string> Images { get; set; } = new();
        public List<ProductOptionDto> Options { get; set; } = new();
    }

    public class ProductOptionDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public int Price { get; set; }
    }
}
