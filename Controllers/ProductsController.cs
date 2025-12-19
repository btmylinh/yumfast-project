using Microsoft.AspNetCore.Mvc;
using WebApp.Services;
using Npgsql;
using System.Text.Json;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/products")] // Giữ nguyên route public
    public class ProductsController : BaseController
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public ProductsController(
            IJsonLocalizationService localizationService,
            IConfiguration config,
            IWebHostEnvironment env
        ) : base(localizationService)
        {
            _config = config;
            _env = env;
        }

        // ============================================================
        // ===================== PUBLIC API ============================
        // ============================================================

        // Danh sách sản phẩm public
        // GET /api/products
        [HttpGet("")]
        public IActionResult Get(
            [FromQuery] string? category,
            [FromQuery] int? min,
            [FromQuery] int? max,
            [FromQuery] string? sort,
            [FromQuery] string? q,
            [FromQuery] string? tag)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var where = "WHERE p.status=1";

            if (!string.IsNullOrWhiteSpace(category)) where += " AND c.name=@cat";
            if (min.HasValue) where += " AND p.price>=@min";
            if (max.HasValue) where += " AND p.price<=@max";
            if (!string.IsNullOrWhiteSpace(q)) where += " AND (p.name ILIKE @q OR p.description ILIKE @q)";
            if (!string.IsNullOrWhiteSpace(tag))
                where += " AND EXISTS (SELECT 1 FROM product_options po WHERE po.product_id=p.id AND (po.type ILIKE @tag OR po.name ILIKE @tag))";

            var order = "ORDER BY p.created_at DESC";
            if (sort == "price_asc") order = "ORDER BY p.price ASC";
            if (sort == "price_desc") order = "ORDER BY p.price DESC";

            using var cmd = new NpgsqlCommand($@"
                SELECT p.id,p.name,c.name,p.price,
                       COALESCE(p.images->>0,'')
                FROM products p
                JOIN categories c ON p.category_id=c.id
                {where} {order}
            ", conn);

            if (!string.IsNullOrWhiteSpace(category)) cmd.Parameters.AddWithValue("@cat", category);
            if (min.HasValue) cmd.Parameters.AddWithValue("@min", min.Value);
            if (max.HasValue) cmd.Parameters.AddWithValue("@max", max.Value);
            if (!string.IsNullOrWhiteSpace(q)) cmd.Parameters.AddWithValue("@q", $"%{q}%");
            if (!string.IsNullOrWhiteSpace(tag)) cmd.Parameters.AddWithValue("@tag", $"%{tag}%");

            var list = new List<ProductDto>();
            using var reader = cmd.ExecuteReader();

            while (reader.Read())
            {
                list.Add(new ProductDto
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Category = reader.GetString(2),
                    Price = reader.GetInt32(3),
                    Image = ResolveImage(reader.IsDBNull(4) ? "" : reader.GetString(4))
                });
            }

            return Ok(new { data = list });
        }

        // Sản phẩm mới nhất
        // GET /api/products/new-products
        [HttpGet("new-products")]
        public IActionResult GetNewProducts([FromQuery] int limit = 12)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT p.id,p.name,c.name,p.price,
                       COALESCE(p.images->>0,'')
                FROM products p
                JOIN categories c ON p.category_id=c.id
                WHERE p.status=1
                ORDER BY p.created_at DESC
                LIMIT @limit
            ", conn);

            cmd.Parameters.AddWithValue("@limit", limit);

            return Ok(new { data = ReadProducts(cmd) });
        }

        // Sản phẩm bán chạy
        // GET /api/products/best-selling
        [HttpGet("best-selling")]
        public IActionResult GetBestSelling([FromQuery] int limit = 12)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT p.id,p.name,c.name,p.price,
                       COALESCE(p.images->>0,'')
                FROM products p
                JOIN categories c ON p.category_id=c.id
                WHERE p.status=1
                ORDER BY p.id DESC
                LIMIT @limit
            ", conn);

            cmd.Parameters.AddWithValue("@limit", limit);

            return Ok(new { data = ReadProducts(cmd) });
        }

        // Autocomplete/Suggestions
        // GET /api/products/autocomplete?q=burger
        [HttpGet("autocomplete")]
        public IActionResult GetAutocomplete([FromQuery] string? q, [FromQuery] int limit = 10)
        {
            if (string.IsNullOrWhiteSpace(q) || q.Length < 2)
                return Ok(new { suggestions = new List<object>() });

            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            // Fuzzy search với PostgreSQL: sử dụng similarity và unaccent
            // Tìm kiếm trong name và description
            var searchTerm = q.Trim().ToLower();
            
            // Tạo các pattern để fuzzy matching
            var patterns = new List<string> { $"%{searchTerm}%" };
            
            // Thêm các biến thể không dấu (nếu có thể)
            var noAccent = RemoveVietnameseAccents(searchTerm);
            if (noAccent != searchTerm)
                patterns.Add($"%{noAccent}%");

            // Query với ranking: exact match > starts with > contains > fuzzy
            using var cmd = new NpgsqlCommand(@"
                SELECT 
                    p.id,
                    p.name,
                    c.name AS category,
                    p.price,
                    COALESCE(p.images->>0,'') AS image,
                    CASE 
                        WHEN LOWER(p.name) = @q THEN 1
                        WHEN LOWER(p.name) LIKE @qStart THEN 2
                        WHEN LOWER(p.name) LIKE @qContains THEN 3
                        ELSE 4
                    END AS rank
                FROM products p
                JOIN categories c ON p.category_id = c.id
                WHERE p.status = 1
                  AND (
                    LOWER(p.name) LIKE @qContains
                    OR LOWER(p.name) LIKE @qNoAccent
                    OR LOWER(p.description) LIKE @qContains
                    OR LOWER(c.name) LIKE @qContains
                  )
                ORDER BY rank ASC, p.name ASC
                LIMIT @limit", conn);

            cmd.Parameters.AddWithValue("@q", searchTerm);
            cmd.Parameters.AddWithValue("@qStart", $"{searchTerm}%");
            cmd.Parameters.AddWithValue("@qContains", $"%{searchTerm}%");
            cmd.Parameters.AddWithValue("@qNoAccent", $"%{noAccent}%");
            cmd.Parameters.AddWithValue("@limit", limit);

            var suggestions = new List<object>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                suggestions.Add(new
                {
                    id = reader.GetInt64(0),
                    name = reader.GetString(1),
                    category = reader.GetString(2),
                    price = reader.GetInt32(3),
                    image = ResolveImage(reader.IsDBNull(4) ? "" : reader.GetString(4)),
                    rank = reader.GetInt32(5)
                });
            }

            return Ok(new { suggestions });
        }

        // Advanced Search với fuzzy matching
        // GET /api/products/search?q=burger&fuzzy=true
        [HttpGet("search")]
        public IActionResult AdvancedSearch(
            [FromQuery] string? q,
            [FromQuery] bool fuzzy = true,
            [FromQuery] string? category = null,
            [FromQuery] int? minPrice = null,
            [FromQuery] int? maxPrice = null,
            [FromQuery] int page = 1,
            [FromQuery] int pageSize = 20)
        {
            if (string.IsNullOrWhiteSpace(q))
                return BadRequest(new { message = "Query is required" });

            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var searchTerm = q.Trim().ToLower();
            var noAccent = RemoveVietnameseAccents(searchTerm);

            var where = "WHERE p.status = 1";
            var orderBy = "ORDER BY ";

            if (fuzzy)
            {
                // Fuzzy search: tìm trong name, description, category
                where += @" AND (
                    LOWER(p.name) LIKE @qContains
                    OR LOWER(p.name) LIKE @qNoAccent
                    OR LOWER(p.description) LIKE @qContains
                    OR LOWER(c.name) LIKE @qContains
                )";
                // Ranking: exact match > starts with > contains
                orderBy += @"CASE 
                    WHEN LOWER(p.name) = @q THEN 1
                    WHEN LOWER(p.name) LIKE @qStart THEN 2
                    WHEN LOWER(p.name) LIKE @qContains THEN 3
                    ELSE 4
                END ASC, p.name ASC";
            }
            else
            {
                // Exact search
                where += " AND (LOWER(p.name) LIKE @qContains OR LOWER(p.description) LIKE @qContains)";
                orderBy += "p.created_at DESC";
            }

            if (!string.IsNullOrWhiteSpace(category))
                where += " AND c.name = @cat";
            if (minPrice.HasValue)
                where += " AND p.price >= @min";
            if (maxPrice.HasValue)
                where += " AND p.price <= @max";

            // Count total
            long total;
            using (var countCmd = new NpgsqlCommand($@"
                SELECT COUNT(*)
                FROM products p
                JOIN categories c ON p.category_id = c.id
                {where}", conn))
            {
                countCmd.Parameters.AddWithValue("@q", searchTerm);
                countCmd.Parameters.AddWithValue("@qStart", $"{searchTerm}%");
                countCmd.Parameters.AddWithValue("@qContains", $"%{searchTerm}%");
                countCmd.Parameters.AddWithValue("@qNoAccent", $"%{noAccent}%");
                if (!string.IsNullOrWhiteSpace(category))
                    countCmd.Parameters.AddWithValue("@cat", category);
                if (minPrice.HasValue)
                    countCmd.Parameters.AddWithValue("@min", minPrice.Value);
                if (maxPrice.HasValue)
                    countCmd.Parameters.AddWithValue("@max", maxPrice.Value);
                var countResult = countCmd.ExecuteScalar();
                total = countResult != null ? (long)countResult : 0L;
            }

            // Get results
            var offset = (page - 1) * pageSize;
            using var cmd = new NpgsqlCommand($@"
                SELECT 
                    p.id, p.name, c.name AS category, p.price,
                    COALESCE(p.images->>0,'') AS image,
                    CASE 
                        WHEN LOWER(p.name) = @q THEN 1
                        WHEN LOWER(p.name) LIKE @qStart THEN 2
                        WHEN LOWER(p.name) LIKE @qContains THEN 3
                        ELSE 4
                    END AS rank
                FROM products p
                JOIN categories c ON p.category_id = c.id
                {where}
                {orderBy}
                LIMIT @limit OFFSET @offset", conn);

            cmd.Parameters.AddWithValue("@q", searchTerm);
            cmd.Parameters.AddWithValue("@qStart", $"{searchTerm}%");
            cmd.Parameters.AddWithValue("@qContains", $"%{searchTerm}%");
            cmd.Parameters.AddWithValue("@qNoAccent", $"%{noAccent}%");
            if (!string.IsNullOrWhiteSpace(category))
                cmd.Parameters.AddWithValue("@cat", category);
            if (minPrice.HasValue)
                cmd.Parameters.AddWithValue("@min", minPrice.Value);
            if (maxPrice.HasValue)
                cmd.Parameters.AddWithValue("@max", maxPrice.Value);
            cmd.Parameters.AddWithValue("@limit", pageSize);
            cmd.Parameters.AddWithValue("@offset", offset);

            var results = new List<ProductDto>();
            using var reader = cmd.ExecuteReader();
            while (reader.Read())
            {
                results.Add(new ProductDto
                {
                    Id = reader.GetInt32(0),
                    Name = reader.GetString(1),
                    Category = reader.GetString(2),
                    Price = reader.GetInt32(3),
                    Image = ResolveImage(reader.IsDBNull(4) ? "" : reader.GetString(4))
                });
            }

            // Log search (simple analytics) - optional, skip if table doesn't exist
            try
            {
                using var logCmd = new NpgsqlCommand(@"
                    INSERT INTO search_logs (query, result_count, created_at)
                    VALUES (@query, @count, NOW())
                    ON CONFLICT DO NOTHING", conn);
                logCmd.Parameters.AddWithValue("@query", searchTerm);
                logCmd.Parameters.AddWithValue("@count", results.Count);
                logCmd.ExecuteNonQuery();
            }
            catch
            {
                // Ignore if table doesn't exist - this is optional
            }

            return Ok(new { data = results, total, page, pageSize });
        }

        // Helper: Remove Vietnamese accents for fuzzy search
        private string RemoveVietnameseAccents(string text)
        {
            if (string.IsNullOrEmpty(text)) return text;
            
            var normalized = text.Normalize(System.Text.NormalizationForm.FormD);
            var sb = new System.Text.StringBuilder();
            
            foreach (var c in normalized)
            {
                var unicodeCategory = System.Globalization.CharUnicodeInfo.GetUnicodeCategory(c);
                if (unicodeCategory != System.Globalization.UnicodeCategory.NonSpacingMark)
                {
                    sb.Append(c);
                }
            }
            
            return sb.ToString().Normalize(System.Text.NormalizationForm.FormC);
        }

        // Deal of the day
        // GET /api/products/deal-of-day
        [HttpGet("deal-of-day")]
        public IActionResult GetDealOfDay([FromQuery] int limit = 12)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT p.id,p.name,c.name,p.price,
                       COALESCE(p.images->>0,'')
                FROM products p
                JOIN categories c ON p.category_id=c.id
                WHERE p.status=1
                ORDER BY p.price ASC
                LIMIT @limit
            ", conn);

            cmd.Parameters.AddWithValue("@limit", limit);

            return Ok(new { data = ReadProducts(cmd) });
        }

        // Chi tiết sản phẩm
        // GET /api/products/{id}
        [HttpGet("{id}")]
        public IActionResult GetById(long id)
        {
            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            using var cmd = new NpgsqlCommand(@"
                SELECT p.id,p.name,c.name,p.description,p.price,p.images
                FROM products p
                JOIN categories c ON p.category_id=c.id
                WHERE p.id=@id
            ", conn);

            cmd.Parameters.AddWithValue("@id", id);

            using var reader = cmd.ExecuteReader();
            if (!reader.Read())
                return NotFound(new { message = "not_found" });

            var imagesJson = reader.IsDBNull(5) ? "[]" : reader.GetString(5);
            var images = JsonSerializer.Deserialize<List<string>>(imagesJson) ?? new();

            var dto = new ProductDetailDto
            {
                Id = reader.GetInt32(0),
                Name = reader.GetString(1),
                Category = reader.GetString(2),
                Description = reader.GetString(3),
                Price = reader.GetInt32(4),
                Images = images.Select(img => ResolveImage(img)).ToList()
            };

            reader.Close();

            // Lấy options
            using var opt = new NpgsqlCommand(@"
                SELECT name,type,price
                FROM product_options
                WHERE product_id=@pid
                ORDER BY type,price
            ", conn);

            opt.Parameters.AddWithValue("@pid", id);

            using var r2 = opt.ExecuteReader();
            dto.Options = new();

            while (r2.Read())
            {
                dto.Options.Add(new ProductOptionDto
                {
                    Name = r2.GetString(0),
                    Type = r2.GetString(1),
                    Price = r2.GetInt32(2)
                });
            }

            return Ok(new { data = dto });
        }


        // ==================== HÀM PHỤ TRỢ ====================

        private List<ProductDto> ReadProducts(NpgsqlCommand cmd)
        {
            var list = new List<ProductDto>();
            using var r = cmd.ExecuteReader();

            while (r.Read())
            {
                list.Add(new ProductDto
                {
                    Id = r.GetInt32(0),
                    Name = r.GetString(1),
                    Category = r.GetString(2),
                    Price = r.GetInt32(3),
                    Image = ResolveImage(r.IsDBNull(4) ? "" : r.GetString(4))
                });
            }
            return list;
        }

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
        // ===================== ADMIN INVENTORY API =====================
        // GET /api/products/stock?search=&lowStockOnly=1
        [HttpGet("stock")]
        public IActionResult GetStock([FromQuery] string? search, [FromQuery] int? lowStockOnly, [FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            using var conn = new Npgsql.NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var baseQuery = $@"
                FROM products p
                JOIN categories c ON p.category_id=c.id
                LEFT JOIN product_stock ps ON ps.product_id=p.id
            ";

            var where = "WHERE p.status=1";
            if (!string.IsNullOrWhiteSpace(search)) where += " AND (p.name ILIKE @s OR c.name ILIKE @s)";
            if (lowStockOnly == 1) where += " AND (ps.quantity - ps.reserved) <= ps.low_stock_threshold";

            // Get total count
            long totalCount;
            using (var countCmd = new Npgsql.NpgsqlCommand($"SELECT COUNT(p.id) {baseQuery} {where}", conn))
            {
                if (!string.IsNullOrWhiteSpace(search)) countCmd.Parameters.AddWithValue("@s", $"%{search}%");
                var countResult = countCmd.ExecuteScalar();
                totalCount = countResult != null ? (long)countResult : 0L;
            }

            var offset = (page - 1) * pageSize;
            var sql = $@"
                SELECT p.id, p.name, c.name AS category,
                       COALESCE(ps.quantity,0) AS quantity,
                       COALESCE(ps.reserved,0) AS reserved,
                       COALESCE(ps.low_stock_threshold,0) AS threshold
                {baseQuery}
                {where}
                ORDER BY p.name ASC
                LIMIT @pageSize OFFSET @offset";

            using var cmd = new Npgsql.NpgsqlCommand(sql, conn);
            if (!string.IsNullOrWhiteSpace(search)) cmd.Parameters.AddWithValue("@s", $"%{search}%");
            cmd.Parameters.AddWithValue("@pageSize", pageSize);
            cmd.Parameters.AddWithValue("@offset", offset);

            var list = new List<object>();
            using var r = cmd.ExecuteReader();
            while (r.Read())
            {
                var qty = r.GetInt32(3);
                var resv = r.GetInt32(4);
                var threshold = r.GetInt32(5);
                var available = qty - resv;
                var isLow = available <= threshold;
                list.Add(new
                {
                    id = r.GetInt32(0),
                    name = r.GetString(1),
                    category = r.GetString(2),
                    quantity = qty,
                    reserved = resv,
                    available,
                    threshold,
                    low = isLow
                });
            }

            return Ok(new { data = list, total = totalCount });
        }

        public class StockAdjustRequest
        {
            public int ProductId { get; set; }
            public string Type { get; set; } = "import"; // import|export|adjust|set-threshold
            public int Quantity { get; set; } = 0; // for import/export is delta; for adjust is new quantity
            public int? Threshold { get; set; }
            public string? Note { get; set; }
        }

        // POST /api/products/stock/adjust
        [HttpPost("stock/adjust")]
        public IActionResult AdjustStock([FromBody] StockAdjustRequest req)
        {
            if (req.ProductId <= 0) return BadRequest(new { message = "invalid_product" });
            using var conn = new Npgsql.NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            // Ensure row exists
            using (var ensure = new Npgsql.NpgsqlCommand(@"
                INSERT INTO product_stock(product_id, quantity, reserved, low_stock_threshold, status)
                VALUES(@pid, 0, 0, 0, 1)
                ON CONFLICT (product_id) DO NOTHING", conn))
            {
                ensure.Parameters.AddWithValue("@pid", req.ProductId);
                ensure.ExecuteNonQuery();
            }

            int qty = 0, reserved = 0, threshold = 0;
            using (var get = new Npgsql.NpgsqlCommand("SELECT quantity, reserved, COALESCE(low_stock_threshold,0) FROM product_stock WHERE product_id=@pid", conn))
            {
                get.Parameters.AddWithValue("@pid", req.ProductId);
                using var r = get.ExecuteReader();
                if (r.Read())
                {
                    qty = r.GetInt32(0);
                    reserved = r.GetInt32(1);
                    threshold = r.GetInt32(2);
                }
            }

            var t = (req.Type ?? "import").ToLower();
            if (t == "import") qty += Math.Max(0, req.Quantity);
            else if (t == "export") qty = Math.Max(0, qty - Math.Max(0, req.Quantity));
            else if (t == "adjust") qty = Math.Max(0, req.Quantity);

            if (req.Threshold.HasValue) threshold = Math.Max(0, req.Threshold.Value);

            using (var upd = new Npgsql.NpgsqlCommand("UPDATE product_stock SET quantity=@q, low_stock_threshold=@th, updated_at=NOW() WHERE product_id=@pid", conn))
            {
                upd.Parameters.AddWithValue("@q", qty);
                upd.Parameters.AddWithValue("@th", threshold);
                upd.Parameters.AddWithValue("@pid", req.ProductId);
                upd.ExecuteNonQuery();
            }

            return Ok(new { productId = req.ProductId, quantity = qty, reserved, threshold });
        }
    }
    // ==================== DTO PUBLIC ====================

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

