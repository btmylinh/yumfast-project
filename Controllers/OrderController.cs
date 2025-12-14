using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers
{
    /// <summary>
    /// Controller MVC đơn giản cho các trang Order, Review, History
    /// </summary>
    [Authorize]
    public class OrderController : Controller
    {
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
    }
}
