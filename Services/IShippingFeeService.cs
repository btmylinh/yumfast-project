using Npgsql;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;

namespace WebApp.Services
{
    // DTO used by service layer to avoid entity conflicts with Models.ShippingZone
    public record ShippingZoneInfo(long Id, string Code, string Name, int BaseFee, int FreeMinimum, short Status);

    public interface IShippingFeeService
    {
        Task<(ShippingZoneInfo? zone, int fee)> QuoteByAddressAsync(long addressId, int subtotal);
        Task<(ShippingZoneInfo? zone, int fee)> QuoteByZoneAsync(long zoneId, int subtotal);
    }
}

