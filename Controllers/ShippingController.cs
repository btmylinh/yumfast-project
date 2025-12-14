using Microsoft.AspNetCore.Mvc;
using WebApp.Services;

namespace WebApp.Controllers
{
    [ApiController]
    [Route("api/shipping")] 
    public class ShippingController : ControllerBase
    {
        private readonly IShippingFeeService _shipping;

        public ShippingController(IShippingFeeService shipping)
        {
            _shipping = shipping;
        }

        // --------------------------------------------------------------------
        // API: Tính phí vận chuyển
        // GET /api/shipping/quote?addressId=...&subtotal=...
        // GET /api/shipping/quote?zoneId=...&subtotal=...
        //
        // subtotal >= 0
        // addressId hoặc zoneId bắt buộc có ít nhất 1
        // --------------------------------------------------------------------
        [HttpGet("quote")]
        public async Task<IActionResult> Quote(
            [FromQuery] long? addressId,
            [FromQuery] long? zoneId,
            [FromQuery] int? subtotal)
        {
            // subtotal không hợp lệ
            if (!subtotal.HasValue || subtotal.Value < 0)
            {
                return BadRequest(new { message = "invalid_subtotal" });
            }

            // Bắt buộc có addressId hoặc zoneId
            if (!(addressId.HasValue || zoneId.HasValue))
            {
                return BadRequest(new { message = "address_or_zone_required" });
            }

            (ShippingZoneInfo? zone, int fee) result;

            // Tính bằng address trước
            if (addressId.HasValue)
            {
                result = await _shipping.QuoteByAddressAsync(addressId.Value, subtotal.Value);
            }
            else
            {
                result = await _shipping.QuoteByZoneAsync(zoneId!.Value, subtotal.Value);
            }

            // Không tìm thấy zone
            if (result.zone == null)
            {
                return NotFound(new { message = "zone_not_found" });
            }

            // Ước tính ETA đơn giản theo zone
            var etaMinutes = (result.zone.Code.ToUpperInvariant().Contains("NOI") || result.zone.Code.ToUpperInvariant().Contains("INNER")) ? 45 : 60;

            // Trả dữ liệu JSON sạch
            return Ok(new
            {
                data = new
                {
                    zone = new
                    {
                        id = result.zone.Id,
                        code = result.zone.Code,
                        name = result.zone.Name
                    },
                    fee = result.fee,
                    eta = etaMinutes
                }
            });
        }
    }
}
