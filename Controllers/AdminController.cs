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
        return View();
    }

    // GET: Admin/Categories
    public IActionResult Categories()
    {
        return View();
    }

    // GET: Admin/Orders
    public IActionResult Orders()
    {
        return View();
    }

    // GET: Admin/Customers
    public IActionResult Customers()
    {
        return View();
    }

    // GET: Admin/Reports
    public IActionResult Reports()
    {
        return View();
    }

    // GET: Admin/Reviews
    public IActionResult Reviews()
    {
        return View();
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
        return View("~/Views/Admin/Customers/CreateCustomer.cshtml");
    }

    // GET: Admin/EditUser
    public IActionResult EditUser()
    {
        return View("~/Views/Admin/Customers/EditCustomer.cshtml");
    }

    // GET: Admin/OrderDetail
    public IActionResult OrderDetail()
    {
        return View("~/Views/Admin/Orders/OrderDetail.cshtml");
    }
}
