using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Localization;
using Microsoft.EntityFrameworkCore;
using WebApp.Models;
using WebApp.Services;
using WebApp.Data;

namespace WebApp.Controllers;

public class HomeController : BaseController
{
    private readonly ApplicationDbContext _context;

    public HomeController(IJsonLocalizationService localizationService, ApplicationDbContext context) : base(localizationService)
    {
        _context = context;
    }
    
    public async Task<IActionResult> Index()
    {
        // Lấy danh sách banner đang hoạt động từ database
        var banners = await _context.Banners
            .Where(b => b.Status == 1) // Chỉ lấy banner đang hoạt động
            .OrderBy(b => b.CreatedAt)
            .ToListAsync();

        return View(banners);
    }

    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
    }

    // POST: Home/SetLanguage
    [HttpPost]
    public IActionResult SetLanguage(string culture, string returnUrl)
    {
        Response.Cookies.Append(
            CookieRequestCultureProvider.DefaultCookieName,
            CookieRequestCultureProvider.MakeCookieValue(new RequestCulture(culture)),
            new CookieOptions { Expires = DateTimeOffset.UtcNow.AddYears(1) }
        );

        return LocalRedirect(returnUrl ?? "/");
    }
}
