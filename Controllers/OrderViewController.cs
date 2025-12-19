using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services;

namespace WebApp.Controllers
{
    /// <summary>
    /// Controller MVC cho các trang liên quan đến đơn hàng của user (Views)
    /// Khác với OrdersController (API), controller này trả về Views
    /// </summary>
    [Authorize]
    public class OrderViewController : BaseController
    {
        public OrderViewController(IJsonLocalizationService localizationService) : base(localizationService)
        {
        }

        /// <summary>
        /// Trang tracking đơn hàng realtime
        /// GET /OrderView/Tracking/{id}
        /// </summary>
        public IActionResult Tracking(long id)
        {
            ViewBag.OrderId = id;
            return View("~/Views/Orders/Tracking.cshtml");
        }

        /// <summary>
        /// Trang đánh giá đơn hàng sau khi hoàn thành
        /// GET /OrderView/Review/{id}
        /// </summary>
        public IActionResult Review(long id)
        {
            ViewBag.OrderId = id;
            return View("~/Views/Orders/Review.cshtml");
        }

        /// <summary>
        /// Trang lịch sử đơn hàng của user
        /// GET /OrderView/MyOrders
        /// </summary>
        public IActionResult MyOrders()
        {
            return View("~/Views/Orders/MyOrders.cshtml");
        }
    }
}
