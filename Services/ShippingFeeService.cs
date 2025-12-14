using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using Npgsql;

namespace WebApp.Services
{
    public class ShippingFeeService : IShippingFeeService
    {
        private readonly IConfiguration _config;
        public ShippingFeeService(IConfiguration config)
        {
            _config = config;
        }

        public async Task<(ShippingZoneInfo? zone, int fee)> QuoteByAddressAsync(long addressId, int subtotal)
        {
            await using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();

            long? zoneId = null;
            await using (var cmd = new NpgsqlCommand("SELECT zone_id FROM user_addresses WHERE id=@id", conn))
            {
                cmd.Parameters.AddWithValue("@id", addressId);
                var obj = await cmd.ExecuteScalarAsync();
                if (obj == null || obj == System.DBNull.Value)
                {
                    return (null, 0);
                }
                zoneId = (obj is long l) ? l : (long?)null;
            }
            if (!zoneId.HasValue)
            {
                return (null, 0);
            }
            return await QuoteByZoneAsync(zoneId.Value, subtotal);
        }

        public async Task<(ShippingZoneInfo? zone, int fee)> QuoteByZoneAsync(long zoneId, int subtotal)
        {
            await using var conn = new NpgsqlConnection(_config.GetConnectionString("DefaultConnection"));
            await conn.OpenAsync();
            await using var cmd = new NpgsqlCommand("SELECT id, code, name, base_fee, COALESCE(free_minimum,0), status FROM shipping_zones WHERE id=@id AND status=1", conn);
            cmd.Parameters.AddWithValue("@id", zoneId);
            await using var r = await cmd.ExecuteReaderAsync();
            if (!await r.ReadAsync())
            {
                return (null, 0);
            }
            var zone = new ShippingZoneInfo(
                r.GetInt64(0),
                r.GetString(1),
                r.GetString(2),
                r.GetInt32(3),
                r.IsDBNull(4) ? 0 : r.GetInt32(4),
                r.GetInt16(5)
            );
            var fee = (subtotal >= zone.FreeMinimum && zone.FreeMinimum > 0) ? 0 : zone.BaseFee;
            return (zone, fee);
        }
    }
}

