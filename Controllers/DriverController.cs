using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace WebApp.Controllers;

/// <summary>
/// Controller cho giao diện Driver (MVC Views)
/// </summary>
[Authorize(Roles = "driver")]
public class DriverController : Controller
{
    private readonly ILogger<DriverController> _logger;

    public DriverController(ILogger<DriverController> logger)
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
