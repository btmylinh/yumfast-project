using Microsoft.AspNetCore.Mvc;
using WebApp.Services;
using WebApp.Models;
using Microsoft.Extensions.DependencyInjection;

namespace WebApp.Controllers;

public class ShopController : BaseController
{
    private readonly IProductCatalogService _catalog;

    [ActivatorUtilitiesConstructor]
    public ShopController(IJsonLocalizationService localizationService, IProductCatalogService catalog) : base(localizationService)
    {
        _catalog = catalog;
    }

    public IActionResult Index(string? category, decimal? min, decimal? max, string? sort, string? q)
    {
        return RedirectToAction("Category", new { category, min, max, sort, q });
    }

    // GET: /shop/category
    public IActionResult Category(string? category, decimal? min, decimal? max, string? sort, string? q)
    {
        var products = _catalog.Filter(category, min, max, sort);
        var vm = new ShopCategoryViewModel
        {
            Products = products,
            Categories = _catalog.GetCategories(),
            SelectedCategory = category,
            MinPrice = min,
            MaxPrice = max,
            Sort = sort
        };
        return View(vm);
    }
}
