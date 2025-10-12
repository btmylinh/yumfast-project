using Microsoft.AspNetCore.Mvc;
using WebApp.Services;

namespace WebApp.Controllers
{
public class DashboardController : BaseController
{
    public DashboardController(IJsonLocalizationService localizationService) : base(localizationService)
    {
    }

        // GET: Dashboard
    public IActionResult Index()
    {
            return View();
        }

        // GET: Dashboard/Banners
        public IActionResult Banners()
        {
            return View();
        }
    }
}