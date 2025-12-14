using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Configuration;
using Npgsql;
using NpgsqlTypes;
using System.Text.Json;
using WebApp.Models;

namespace WebApp.Services
{
    internal class CartCookieItem
    {
        public int ProductId { get; set; }
        public int Quantity { get; set; }
        public string? OptionsKey { get; set; }
    }

    public interface ICheckoutService
    {
        Task<object> CheckoutAsync(CheckoutRequest req, string cartJson, HttpResponse response);
    }

    public class CheckoutService : ICheckoutService
    {
        private readonly IConfiguration _config;
        private readonly IShippingFeeService _shipping;

        public CheckoutService(IConfiguration config, IShippingFeeService shipping)
        {
            _config = config;
            _shipping = shipping;
        }

        // ---------------------------------------------------------
        // Xử lý checkout đầy đủ
        // ---------------------------------------------------------
        public async Task<object> CheckoutAsync(CheckoutRequest req, string cartJson, HttpResponse response)
        {
            var items = JsonSerializer.Deserialize<List<CartCookieItem>>(cartJson) ?? new();

            if (items.Count == 0)
                return new { message = "cart_empty" };

            await using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            await using var tx = await conn.BeginTransactionAsync();

            try
            {
                // -------- Load & validate product / subtotal --------
                var details = new List<(long productId, string name, int price, int qty, string? optionsJson)>();
                var subtotal = 0;
                var outOfStock = new List<object>();

                foreach (var it in items)
                {
                    await using var pcmd = new NpgsqlCommand(
                        "SELECT id, name, price, status FROM products WHERE id=@id",
                        conn, (NpgsqlTransaction)tx
                    );

                    pcmd.Parameters.AddWithValue("@id", it.ProductId);

                    await using var pr = await pcmd.ExecuteReaderAsync();
                    if (!await pr.ReadAsync())
                    {
                        await pr.DisposeAsync();
                        continue;
                    }

                    var pid = pr.GetInt32(0);
                    var name = pr.GetString(1);
                    var price = pr.GetInt32(2);
                    var status = pr.GetInt16(3);
                    await pr.DisposeAsync();

                    if (status != 1) continue;

                    // Stock check
                    await using (var scmd = new NpgsqlCommand(
                        "SELECT quantity, reserved FROM product_stock WHERE product_id=@pid",
                        conn, (NpgsqlTransaction)tx))
                    {
                        scmd.Parameters.AddWithValue("@pid", (long)pid);

                        await using var sr = await scmd.ExecuteReaderAsync();
                        if (await sr.ReadAsync())
                        {
                            var qty = sr.GetInt32(0);
                            var reserved = sr.GetInt32(1);
                            var available = qty - reserved;

                            if (available < it.Quantity)
                            {
                                outOfStock.Add(new { productId = pid, available, requested = it.Quantity });
                            }
                        }

                        await sr.DisposeAsync();
                    }

                    var lineTotal = price * it.Quantity;
                    subtotal += lineTotal;

                    details.Add(((long)pid, name, price, it.Quantity, it.OptionsKey));
                }

                if (outOfStock.Count > 0)
                {
                    await tx.RollbackAsync();
                    return new { message = "out_of_stock", items = outOfStock };
                }

                if (subtotal <= 0)
                {
                    await tx.RollbackAsync();
                    return new { message = "subtotal_invalid" };
                }

                // -------- Shipping fee --------
                var (zone, shippingFee) = await _shipping.QuoteByAddressAsync(req.AddressId, subtotal);
                if (zone == null)
                {
                    await tx.RollbackAsync();
                    return new { message = "shipping_zone_not_found" };
                }

                // -------- Coupon --------
                long? couponId = null;
                var discount = 0;

                if (!string.IsNullOrWhiteSpace(req.CouponCode))
                {
                    await using var cc = new NpgsqlCommand(@"
                        SELECT id, type, value, product_id
                          FROM coupons
                         WHERE LOWER(code)=LOWER(@code)
                           AND status=1
                           AND start_at <= NOW() 
                           AND end_at > NOW()
                           AND used_count < total",
                        conn, (NpgsqlTransaction)tx);

                    cc.Parameters.AddWithValue("@code", req.CouponCode.Trim());

                    await using var cr = await cc.ExecuteReaderAsync();
                    if (await cr.ReadAsync())
                    {
                        couponId = cr.GetInt64(0);
                        var type = cr.GetInt16(1);
                        var value = cr.GetInt32(2);
                        var targetPid = cr.IsDBNull(3) ? (long?)null : cr.GetInt64(3);

                        await cr.DisposeAsync();

                        int applicableSubtotal = subtotal;

                        if (targetPid.HasValue)
                        {
                            applicableSubtotal = details
                                .Where(x => x.productId == targetPid.Value)
                                .Sum(x => x.price * x.qty);
                        }

                        if (applicableSubtotal > 0)
                        {
                            discount = type == 1
                                ? (int)Math.Floor(applicableSubtotal * (value / 100.0))
                                : Math.Min(value, applicableSubtotal);
                        }
                    }
                    else await cr.DisposeAsync();
                }

                // -------- Create order base info --------
                var code = $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString("N")[..6].ToUpper()}";

                string shipName = "", shipPhone = "", shipAddress = "";
                await using (var acmd = new NpgsqlCommand(
                    "SELECT name, phone, address FROM user_addresses WHERE id=@id",
                    conn, (NpgsqlTransaction)tx))
                {
                    acmd.Parameters.AddWithValue("@id", req.AddressId);
                    await using var ar = await acmd.ExecuteReaderAsync();

                    if (!await ar.ReadAsync())
                    {
                        await tx.RollbackAsync();
                        return new { message = "address_not_found" };
                    }

                    shipName = ar.GetString(0);
                    shipPhone = ar.GetString(1);
                    shipAddress = ar.GetString(2);

                    await ar.DisposeAsync();
                }

                var total = Math.Max(0, subtotal - discount) + shippingFee;

                long orderId;

                await using (var ocmd = new NpgsqlCommand(@"
                    INSERT INTO orders
                        (code, user_id, coupons_id, ship_name, ship_phone, ship_address_text,
                        price_subtotal, price_discount, price_shipping, total_price,
                        payment_status, status)
                    VALUES (@code, NULL, @cid, @sn, @sp, @sa,
                            @sub, @disc, @ship, @total,
                            0, 0)
                    RETURNING id", conn, (NpgsqlTransaction)tx))
                {
                    ocmd.Parameters.AddWithValue("@code", code);
                    ocmd.Parameters.AddWithValue("@cid", (object?)couponId ?? DBNull.Value);
                    ocmd.Parameters.AddWithValue("@sn", shipName);
                    ocmd.Parameters.AddWithValue("@sp", shipPhone);
                    ocmd.Parameters.AddWithValue("@sa", shipAddress);
                    ocmd.Parameters.AddWithValue("@sub", subtotal);
                    ocmd.Parameters.AddWithValue("@disc", discount);
                    ocmd.Parameters.AddWithValue("@ship", shippingFee);
                    ocmd.Parameters.AddWithValue("@total", total);

                    var scalarId = await ocmd.ExecuteScalarAsync();
if (scalarId is long l) orderId = l;
else throw new InvalidOperationException("create_order_failed");
                }

                // -------- Insert order_items --------
                foreach (var d in details)
                {
                    await using var icmd = new NpgsqlCommand(@"
                        INSERT INTO order_items(order_id, product_id, quantity, price, total, options)
                             VALUES (@oid, @pid, @qty, @price, @total, @opt)",
                        conn, (NpgsqlTransaction)tx);

                    icmd.Parameters.AddWithValue("@oid", orderId);
                    icmd.Parameters.AddWithValue("@pid", d.productId);
                    icmd.Parameters.AddWithValue("@qty", d.qty);
                    icmd.Parameters.AddWithValue("@price", d.price);
                    icmd.Parameters.AddWithValue("@total", d.price * d.qty);
                    icmd.Parameters.AddWithValue("@opt", (object?)d.optionsJson ?? DBNull.Value);

                    await icmd.ExecuteNonQueryAsync();
                }

                // -------- Insert order_status_history --------
                await using (var hcmd = new NpgsqlCommand(@"
                    INSERT INTO order_status_history(order_id, status, note, created_by)
                         VALUES (@oid, 0, 'Order placed', NULL)",
                    conn, (NpgsqlTransaction)tx))
                {
                    hcmd.Parameters.AddWithValue("@oid", orderId);
                    await hcmd.ExecuteNonQueryAsync();
                }

                // -------- Insert order_payments --------
                await using (var pay = new NpgsqlCommand(@"
                    INSERT INTO order_payments(order_id, amount, status, method)
                        VALUES(@oid, @amount, 0, @method)",
                    conn, (NpgsqlTransaction)tx))
                {
                    pay.Parameters.AddWithValue("@oid", orderId);
                    pay.Parameters.AddWithValue("@amount", total);
                    pay.Parameters.AddWithValue("@method", req.PaymentMethod.ToUpperInvariant());

                    await pay.ExecuteNonQueryAsync();
                }

                await tx.CommitAsync();

                // -------- Clear cart cookie --------
                response.Cookies.Append("cart", "[]", new CookieOptions
                {
                    HttpOnly = false,
                    IsEssential = true,
                    Expires = DateTimeOffset.UtcNow.AddDays(7)
                });

                return new
                {
                    orderId,
                    code,
                    subtotal,
                    discount,
                    shipping = shippingFee,
                    total
                };
            }
            catch (PostgresException ex)
            {
                await tx.RollbackAsync();
                return new { error = "db_error", detail = ex.MessageText };
            }
        }
    }
}
