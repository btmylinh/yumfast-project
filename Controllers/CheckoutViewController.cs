using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services;

namespace WebApp.Controllers
{
    /// <summary>
    /// MVC Controller cho các trang Checkout Views
    /// </summary>
    [Route("Checkout")]
    public class CheckoutPageController : BaseController
    {
        public CheckoutPageController(IJsonLocalizationService localizationService) : base(localizationService)
        {
        }

        /// <summary>
        /// Trang thanh toán đơn hàng
        /// GET /Checkout/Payment
        /// </summary>
        [HttpGet("Payment")]
        [Authorize(AuthenticationSchemes = "Cookies,Bearer")]
        public IActionResult Payment()
        {
            return View("~/Views/Checkout/Payment.cshtml");
        }
    }
}
