using Microsoft.AspNetCore.Mvc;
using WebApp.Services;

namespace WebApp.Controllers;

public class AdminController : BaseController
{
    public AdminController(IJsonLocalizationService localizationService) : base(localizationService)
    {
    }

    // GET: Admin/Products
    public IActionResult Products()
    {
        return View("~/Views/Admin/Products/Products.cshtml");
    }

    // GET: Admin/Categories
    public IActionResult Categories()
    {
        return View("~/Views/Admin/Categories/Categories.cshtml");
    }

    // GET: Admin/Orders
    public IActionResult Orders()
    {
        return View("~/Views/Admin/Orders/Orders.cshtml");
    }

    // GET: Admin/Users
    public IActionResult Users()
    {
        return View("~/Views/Admin/Users/Users.cshtml");
    }

    // GET: Admin/Reports
    public IActionResult Reports()
    {
        return View("~/Views/Admin/Reports/Reports.cshtml");
    }

    // GET: Admin/Reviews
    public IActionResult Reviews()
    {
        return View("~/Views/Admin/Reviews.cshtml");
    }

    // GET: Admin/AddProduct
    public IActionResult AddProduct()
    {
        return View("~/Views/Admin/Products/AddProduct.cshtml");
    }

    // GET: Admin/AddCategory
    public IActionResult AddCategory()
    {
        return View("~/Views/Admin/Categories/AddCategory.cshtml");
    }

    // GET: Admin/CreateUser
    public IActionResult CreateUser()
    {
        return View("~/Views/Admin/Users/CreateUser.cshtml");
    }

    // GET: Admin/EditUser
    public IActionResult EditUser()
    {
        return View("~/Views/Admin/Users/EditUser.cshtml");
    }

    // GET: Admin/OrderDetail
    public IActionResult OrderDetail()
    {
        return View("~/Views/Admin/Orders/OrderDetail.cshtml");
    }
}
