using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using WebApp.Services;

namespace WebApp.Controllers;

/// <summary>

public class DriverController : BaseController
{
    private readonly ILogger<DriverController> _logger;

    public DriverController(
        ILogger<DriverController> logger,
        IJsonLocalizationService localizationService
    ) : base(localizationService)
    {
        _logger = logger;
    }

    /// <summary>
    /// Trang chủ Driver Dashboard
    /// GET /Driver/Dashboard
    /// </summary>
    public IActionResult Dashboard()
    {
        return View();
    }

    /// <summary>
    /// Chi tiết một đơn hàng
    /// GET /Driver/Order/{id}
    /// </summary>
    public IActionResult Order(long id)
    {
        ViewBag.OrderId = id;
        return View();
    }
}
