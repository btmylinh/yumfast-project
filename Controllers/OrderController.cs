using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services;
using System.Security.Claims;

namespace WebApp.Controllers
{
    /// <summary>
    /// Controller MVC đơn giản cho các trang Order, Review, History
    /// </summary>
    [Authorize(AuthenticationSchemes = "Cookies,Bearer")]
    public class OrderController : BaseController
    {
        public OrderController(IJsonLocalizationService localizationService) : base(localizationService)
        {
        }

        /// <summary>
        /// Trang tracking đơn hàng realtime
        /// GET /Order/Tracking/{id}
        /// </summary>
        public IActionResult Tracking(long id)
        {
            ViewBag.OrderId = id;
            return View();
        }

        /// <summary>
        /// Trang đánh giá đơn hàng sau khi hoàn thành
        /// GET /Order/Review/{id}
        /// </summary>
        public IActionResult Review(long id)
        {
            ViewBag.OrderId = id;
            return View();
        }

        /// <summary>
        /// Trang lịch sử đơn hàng của user
        /// GET /Order/MyOrders
        /// </summary>
        public IActionResult MyOrders()
        {
            return View();
        }

        /// <summary>
        /// Render Order Tracking Card partial view
        /// GET /Order/GetTrackingCard/{orderId}
        /// </summary>
        [HttpGet]
        [Route("Order/GetTrackingCard/{orderId}")]
        public async Task<IActionResult> GetTrackingCard(long orderId)
        {
            try
            {
                // Debug: Log orderId để kiểm tra
                var logger = HttpContext.RequestServices.GetRequiredService<Microsoft.Extensions.Logging.ILogger<OrderController>>();
                logger?.LogInformation("GetTrackingCard called with orderId={OrderId}", orderId);
                
                if (orderId <= 0)
                {
                    logger?.LogWarning("Invalid orderId: {OrderId}", orderId);
                    return NotFound();
                }
                
                // Lấy thông tin order từ service
                var trackingService = HttpContext.RequestServices.GetRequiredService<Services.Interfaces.IOrderTrackingService>();
                var userIdClaim = User.FindFirst("uid")?.Value ?? User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                var userId = long.TryParse(userIdClaim, out var uid) ? uid : (long?)null;
                
                logger?.LogInformation("Getting tracking for orderId={OrderId}, userId={UserId}", orderId, userId);
                var tracking = await trackingService.GetOrderTrackingAsync(orderId, userId);
                if (tracking == null)
                {
                    return NotFound();
                }

                // Map data cho partial view
                var model = new
                {
                    Order = new
                    {
                        Id = tracking.OrderId,
                        OrderId = tracking.OrderId,
                        OrderCode = tracking.OrderCode,
                        Code = tracking.OrderCode,
                        Status = tracking.Status,
                        ShipName = tracking.ShipName,
                        ShipPhone = tracking.ShipPhone,
                        ShipAddress = tracking.ShipAddress,
                        TotalPrice = tracking.TotalPrice,
                        CreatedAt = tracking.CreatedAt,
                        ConfirmedAt = tracking.CreatedAt, // Fallback
                        DriverAcceptedAt = tracking.DriverAcceptedAt,
                        DeliveryStartedAt = tracking.DriverAcceptedAt, // Fallback
                        CompletedAt = tracking.CompletedAt,
                        EstimatedDeliveryTime = "" // Có thể tính từ created_at + estimated time
                    },
                    Driver = tracking.Driver != null ? new
                    {
                        Id = tracking.Driver.Id,
                        FullName = tracking.Driver.FullName ?? "",
                        Name = tracking.Driver.FullName ?? "",
                        Phone = tracking.Driver.Phone ?? ""
                    } : null,
                    Restaurant = new
                    {
                        Name = "", // Cần lấy từ order items
                        Phone = ""
                    }
                };

                return PartialView("~/Views/Shared/_OrderTrackingCard.cshtml", model);
            }
            catch
            {
                return NotFound();
            }
        }
    }
}
