using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting;
using Npgsql;
using System.Text.Json;
using System.Linq;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/cart")]
    public class CartApiController : ControllerBase
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;
        public CartApiController(IConfiguration config, IWebHostEnvironment env) { _config = config; _env = env; }

        public class AddCartRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; } = 1;
            public object? Options { get; set; }
        }

        public class CartUpdateRequest
        {
            public int ProductId { get; set; }
            public string OptionsKey { get; set; } = string.Empty;
            public int Quantity { get; set; }
        }

        [HttpPost("add")]
        public IActionResult Add([FromBody] AddCartRequest req)
        {
            try
            {
                using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();
                var json = JsonSerializer.Serialize(new { product_id = req.ProductId, quantity = req.Quantity, options = req.Options });
                using var cmd = new NpgsqlCommand("INSERT INTO carts(user_id, cart_item) VALUES(NULL, @item) RETURNING id", conn);
                cmd.Parameters.AddWithValue("@item", json);
                var id = (long)cmd.ExecuteScalar();
                return Ok(new { id });
            }
            catch (PostgresException)
            {
                return FallbackAddToCookie(req);
            }
            catch
            {
                return FallbackAddToCookie(req);
            }
        }

        private IActionResult FallbackAddToCookie(AddCartRequest req)
        {
            var cartCookie = Request.Cookies["cart"];
            var items = string.IsNullOrEmpty(cartCookie)
                ? new List<CartCookieItem>()
                : (JsonSerializer.Deserialize<List<CartCookieItem>>(cartCookie) ?? new List<CartCookieItem>());

            var key = JsonSerializer.Serialize(req.Options ?? new { });
            var existing = items.FirstOrDefault(i => i.ProductId == req.ProductId && i.OptionsKey == key);
            if (existing != null)
            {
                existing.Quantity += Math.Max(1, req.Quantity);
            }
            else
            {
                items.Add(new CartCookieItem { ProductId = req.ProductId, Quantity = Math.Max(1, req.Quantity), OptionsKey = key });
            }

            var serialized = JsonSerializer.Serialize(items);
            Response.Cookies.Append("cart", serialized, new CookieOptions
            {
                HttpOnly = false,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
            return Ok(new { code = "added_to_cart" });
        }

        private class CartCookieItem
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
            public string OptionsKey { get; set; } = string.Empty;
        }

        [HttpPost("update")]
        public IActionResult Update([FromBody] List<CartUpdateRequest> req)
        {
            var cartCookie = Request.Cookies["cart"];
            var items = string.IsNullOrEmpty(cartCookie)
                ? new List<CartCookieItem>()
                : (JsonSerializer.Deserialize<List<CartCookieItem>>(cartCookie) ?? new List<CartCookieItem>());

            if (req == null || req.Count == 0)
            {
                return BadRequest(new { message = "empty_request" });
            }

            foreach (var r in req)
            {
                var matches = items.Where(i => i.ProductId == r.ProductId && (string.IsNullOrEmpty(r.OptionsKey) || i.OptionsKey == r.OptionsKey)).ToList();
                if (matches.Count == 0) continue;
                if (r.Quantity <= 0)
                {
                    // remove all matches
                    items = items.Where(i => !(i.ProductId == r.ProductId && (string.IsNullOrEmpty(r.OptionsKey) || i.OptionsKey == r.OptionsKey))).ToList();
                }
                else
                {
                    foreach (var m in matches)
                    {
                        m.Quantity = r.Quantity;
                    }
                }
            }

            var serialized = JsonSerializer.Serialize(items);
            Response.Cookies.Append("cart", serialized, new CookieOptions
            {
                HttpOnly = false,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });
            return Ok(new { code = "cart_updated" });
        }

        [HttpGet]
        public IActionResult Get()
        {
            var cartCookie = Request.Cookies["cart"];
            var items = string.IsNullOrEmpty(cartCookie)
                ? new List<CartCookieItem>()
                : (JsonSerializer.Deserialize<List<CartCookieItem>>(cartCookie) ?? new List<CartCookieItem>());

            if (items.Count == 0)
                return Ok(new { data = Array.Empty<object>(), total = 0 });

            var details = new List<object>();
            var total = 0;

            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();
            foreach (var it in items)
            {
                using var cmd = new NpgsqlCommand("SELECT id, name, price, COALESCE(images->>0,'') AS image FROM products WHERE id=@id", conn);
                cmd.Parameters.AddWithValue("@id", it.ProductId);
                using var r = cmd.ExecuteReader();
                if (r.Read())
                {
                    var id = r.GetInt32(0);
                    var name = r.GetString(1);
                    var price = r.GetInt32(2);
                    var img = r.IsDBNull(3) ? string.Empty : r.GetString(3);
                    var image = string.IsNullOrWhiteSpace(img) ? "/assets/images/docs/placeholder-img.jpg" : (img.StartsWith("/") ? img : $"/assets/images/products/{img}");
                    if (!string.IsNullOrWhiteSpace(image))
                    {
                        var physical = image.StartsWith("/") ? System.IO.Path.Combine(_env.WebRootPath, image.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar)) : System.IO.Path.Combine(_env.WebRootPath, image.Replace('/', System.IO.Path.DirectorySeparatorChar));
                        if (!System.IO.File.Exists(physical))
                        {
                            image = "/assets/images/docs/placeholder-img.jpg";
                        }
                    }
                    var qty = it.Quantity;
                    var subtotal = price * qty;
                    total += subtotal;
                    details.Add(new { id, name, price, image, quantity = qty, subtotal, key = it.OptionsKey });
                }
                else
                {
                    details.Add(new { id = it.ProductId, name = "", price = 0, image = "", quantity = it.Quantity, subtotal = 0, key = it.OptionsKey });
                }
                r.Close();
            }

            return Ok(new { data = details, total });
        }
    }
}
