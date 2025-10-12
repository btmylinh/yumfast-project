using Microsoft.AspNetCore.Mvc;
using WebApp.Services;

namespace WebApp.Controllers;

public class ProductController : BaseController
{
    public ProductController(IJsonLocalizationService localizationService) : base(localizationService)
    {
    }

    public IActionResult Detail(int? id)
    {
        return View("~/Views/Product/Detail.cshtml");
    }
}
