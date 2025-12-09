using Microsoft.AspNetCore.Mvc;
using WebApp.Services;

namespace WebApp.Controllers
{
    public class CouponViewController : BaseController
    {
        public CouponViewController(IJsonLocalizationService localizationService) : base(localizationService) { }

        public IActionResult Index()
        {
            return View("~/Views/Coupons/Index.cshtml");
        }
    }
}
