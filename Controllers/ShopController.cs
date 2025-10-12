using Microsoft.AspNetCore.Mvc;
using WebApp.Services;

namespace WebApp.Controllers;

public class ShopController : BaseController
{
    public ShopController(IJsonLocalizationService localizationService) : base(localizationService)
    {
    }

    public IActionResult Index()
    {
        return View();
    }
}
