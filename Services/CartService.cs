using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Hosting;
using Npgsql;
using System.Text.Json;

namespace WebApp.Services
{
    public class CartService
    {
        private readonly IConfiguration _config;
        private readonly IWebHostEnvironment _env;

        public CartService(IConfiguration config, IWebHostEnvironment env)
        {
            _config = config;
            _env = env;
        }

        // Request thêm sản phẩm vào giỏ
        public class AddCartRequest
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; } = 1;
            public object? Options { get; set; }
        }

        // Request cập nhật số lượng
        public class CartUpdateRequest
        {
            public int ProductId { get; set; }
            public string OptionsKey { get; set; } = string.Empty;
            public int Quantity { get; set; }
        }

        // Item lưu trong cookie
        private class CartCookieItem
        {
            public int ProductId { get; set; }
            public int Quantity { get; set; }
            public string OptionsKey { get; set; } = string.Empty;
        }

        // -----------------------------------------------------
        // Hàm xử lý thêm sản phẩm vào giỏ
        // -----------------------------------------------------
        public object AddToCart(AddCartRequest req, HttpRequest httpReq, HttpResponse httpRes)
        {
            try
            {
                using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
                conn.Open();

                // Kiểm tra sản phẩm có tồn tại & đang active
                using (var check = new NpgsqlCommand("SELECT status FROM products WHERE id=@id", conn))
                {
                    check.Parameters.AddWithValue("@id", req.ProductId);
                    var st = check.ExecuteScalar();

                    if (st == null)
                        return new { error = "product_not_found" };

                    if (Convert.ToInt32(st) != 1)
                        return new { error = "product_inactive" };
                }

                // Lưu vào DB
                var json = JsonSerializer.Serialize(new
                {
                    product_id = req.ProductId,
                    quantity = req.Quantity,
                    options = req.Options
                });

                using var cmd = new NpgsqlCommand("INSERT INTO carts(user_id, cart_item) VALUES(NULL, @item) RETURNING id", conn);
                cmd.Parameters.AddWithValue("@item", json);
                var id = (long)cmd.ExecuteScalar();

                return new { id };
            }
            catch
            {
                return AddToCookie(req, httpReq, httpRes);
            }
        }

        // -----------------------------------------------------
        // Hàm fallback lưu giỏ vào cookie nếu DB lỗi
        // -----------------------------------------------------
        private object AddToCookie(AddCartRequest req, HttpRequest request, HttpResponse response)
        {
            var cartCookie = request.Cookies["cart"];

            var items = string.IsNullOrEmpty(cartCookie)
                ? new List<CartCookieItem>()
                : JsonSerializer.Deserialize<List<CartCookieItem>>(cartCookie) ?? new List<CartCookieItem>();

            var key = JsonSerializer.Serialize(req.Options ?? new { });

            var existing = items.FirstOrDefault(i => i.ProductId == req.ProductId && i.OptionsKey == key);

            // Nếu đã có sản phẩm → cộng dồn số lượng
            if (existing != null)
            {
                existing.Quantity += Math.Max(1, req.Quantity);
            }
            else
            {
                items.Add(new CartCookieItem
                {
                    ProductId = req.ProductId,
                    Quantity = Math.Max(1, req.Quantity),
                    OptionsKey = key
                });
            }

            var serialized = JsonSerializer.Serialize(items);

            response.Cookies.Append("cart", serialized, new CookieOptions
            {
                HttpOnly = false,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            return new { code = "added_to_cart" };
        }

        // -----------------------------------------------------
        // Hàm xử lý cập nhật số lượng
        // -----------------------------------------------------
        public object UpdateCart(List<CartUpdateRequest> req, HttpRequest httpReq, HttpResponse httpRes)
        {
            var cartCookie = httpReq.Cookies["cart"];

            var items = string.IsNullOrEmpty(cartCookie)
                ? new List<CartCookieItem>()
                : JsonSerializer.Deserialize<List<CartCookieItem>>(cartCookie) ?? new List<CartCookieItem>();

            foreach (var r in req)
            {
                if (r.Quantity < 0) r.Quantity = 0;
                if (r.Quantity > 99) r.Quantity = 99;

                var matches = items
                    .Where(i => i.ProductId == r.ProductId && (string.IsNullOrEmpty(r.OptionsKey) || i.OptionsKey == r.OptionsKey))
                    .ToList();

                if (matches.Count == 0) continue;

                // Nếu số lượng = 0 → xóa item
                if (r.Quantity == 0)
                {
                    items = items.Where(i =>
                        !(i.ProductId == r.ProductId &&
                          (string.IsNullOrEmpty(r.OptionsKey) || i.OptionsKey == r.OptionsKey))
                    ).ToList();
                }
                else
                {
                    foreach (var m in matches)
                        m.Quantity = r.Quantity;
                }
            }

            // Lưu lại cookie
            httpRes.Cookies.Append("cart", JsonSerializer.Serialize(items), new CookieOptions
            {
                HttpOnly = false,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            return new { code = "cart_updated" };
        }

        // -----------------------------------------------------
        // Hàm xóa item khỏi giỏ
        // -----------------------------------------------------
        public object RemoveFromCart(int productId, string? key, HttpRequest req, HttpResponse res)
        {
            var cartCookie = req.Cookies["cart"];

            var items = string.IsNullOrEmpty(cartCookie)
                ? new List<CartCookieItem>()
                : JsonSerializer.Deserialize<List<CartCookieItem>>(cartCookie) ?? new List<CartCookieItem>();

            items = items.Where(i =>
                !(i.ProductId == productId &&
                  (string.IsNullOrEmpty(key) || i.OptionsKey == (key ?? "")))
            ).ToList();

            res.Cookies.Append("cart", JsonSerializer.Serialize(items), new CookieOptions
            {
                HttpOnly = false,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            return new { code = "cart_item_removed" };
        }

        // -----------------------------------------------------
        // Hàm xóa toàn bộ giỏ hàng
        // -----------------------------------------------------
        public object ClearCart(HttpResponse res)
        {
            res.Cookies.Append("cart", JsonSerializer.Serialize(new List<CartCookieItem>()), new CookieOptions
            {
                HttpOnly = false,
                IsEssential = true,
                Expires = DateTimeOffset.UtcNow.AddDays(7)
            });

            return new { code = "cart_cleared" };
        }

        // -----------------------------------------------------
        // Hàm lấy tổng số lượng sản phẩm
        // -----------------------------------------------------
        public int CountItems(HttpRequest req)
        {
            var cartCookie = req.Cookies["cart"];

            var items = string.IsNullOrEmpty(cartCookie)
                ? new List<CartCookieItem>()
                : JsonSerializer.Deserialize<List<CartCookieItem>>(cartCookie) ?? new List<CartCookieItem>();

            return items.Sum(i => Math.Max(0, i.Quantity));
        }

        // -----------------------------------------------------
        // Hàm lấy chi tiết giỏ hàng + tổng tiền
        // -----------------------------------------------------
        public object GetCart(HttpRequest req)
        {
            var cartCookie = req.Cookies["cart"];

            var items = string.IsNullOrEmpty(cartCookie)
                ? new List<CartCookieItem>()
                : JsonSerializer.Deserialize<List<CartCookieItem>>(cartCookie) ?? new List<CartCookieItem>();

            if (items.Count == 0)
                return new { data = Array.Empty<object>(), total = 0 };

            using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            conn.Open();

            var details = new List<object>();
            var total = 0;

            foreach (var it in items)
            {
                using var cmd = new NpgsqlCommand(
                    "SELECT id, name, price, COALESCE(images->>0,'') AS image FROM products WHERE id=@id",
                    conn
                );

                cmd.Parameters.AddWithValue("@id", it.ProductId);

                using var r = cmd.ExecuteReader();

                if (r.Read())
                {
                    var id = r.GetInt32(0);
                    var name = r.GetString(1);
                    var price = r.GetInt32(2);
                    var img = r.IsDBNull(3) ? "" : r.GetString(3);

                    var image = string.IsNullOrWhiteSpace(img)
                        ? "/assets/images/docs/placeholder-img.jpg"
                        : (img.StartsWith("/") ? img : $"/assets/images/products/{img}");

                    var physical = System.IO.Path.Combine(
                        _env.WebRootPath,
                        image.TrimStart('/').Replace('/', System.IO.Path.DirectorySeparatorChar)
                    );

                    if (!System.IO.File.Exists(physical))
                        image = "/assets/images/docs/placeholder-img.jpg";

                    var qty = it.Quantity;
                    var subtotal = qty * price;
                    total += subtotal;

                    details.Add(new
                    {
                        id,
                        name,
                        price,
                        image,
                        quantity = qty,
                        subtotal,
                        key = it.OptionsKey
                    });
                }
                else
                {
                    details.Add(new
                    {
                        id = it.ProductId,
                        name = "",
                        price = 0,
                        image = "",
                        quantity = it.Quantity,
                        subtotal = 0,
                        key = it.OptionsKey
                    });
                }
            }

            return new { data = details, total };
        }
    }
}
