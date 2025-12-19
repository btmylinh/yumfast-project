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

    // Product detail MVC routes
    [HttpGet("/product/detail/{id}")]
    public IActionResult ProductDetail(long id)
    {
        return View("~/Views/Product/Detail.cshtml");
    }

    // Short route variant
    [HttpGet("/product/{id}")]
    public IActionResult ProductDetailShort(long id)
    {
        return View("~/Views/Product/Detail.cshtml");
    }

    // Auth views (SignIn / SignUp)
    [HttpGet("/auth/SignIn")]
    public IActionResult AuthSignIn()
    {
        return View("~/Views/Auth/SignIn.cshtml");
    }

    [HttpGet("/auth/SignUp")]
    public IActionResult AuthSignUp()
    {
        return View("~/Views/Auth/SignUp.cshtml");
    }

    [HttpGet("/auth/ForgotPassword")]
    public IActionResult AuthForgotPassword()
    {
        return View("~/Views/Auth/ForgotPassword.cshtml");
    }

    [HttpGet("/auth/ResetPassword")]
    public IActionResult AuthResetPassword()
    {
        return View("~/Views/Auth/ResetPassword.cshtml");
    }

    [HttpGet("/auth/Verify")]
    public IActionResult AuthVerify()
    {
        return View("~/Views/Auth/Verify.cshtml");
    }
    
    /// <summary>
    /// Render Product Review Form partial view
    /// GET /Product/GetReviewForm?productId=123
    /// </summary>
    [HttpGet("/Product/GetReviewForm")]
    public async Task<IActionResult> GetReviewForm([FromQuery] long productId)
    {
        if (productId <= 0)
        {
            return NotFound();
        }
        
        ViewBag.ProductId = productId;
        
        // Check if user can review (if logged in)
        var userIdClaim = User.FindFirst("uid")?.Value ?? User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value;
        if (!string.IsNullOrEmpty(userIdClaim) && long.TryParse(userIdClaim, out var userId))
        {
            var reviewService = HttpContext.RequestServices.GetRequiredService<Services.Interfaces.IProductReviewService>();
            var canReview = await reviewService.CanReviewProductAsync(productId, userId);
            ViewBag.CanReview = canReview;
        }
        else
        {
            ViewBag.CanReview = false;
        }
        
        return PartialView("~/Views/Shared/_ProductReviewForm.cshtml");
    }
}
