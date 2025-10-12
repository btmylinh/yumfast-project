using Microsoft.AspNetCore.Mvc;
using WebApp.Services;

namespace WebApp.Controllers;

public class UserController : BaseController
{
    public UserController(IJsonLocalizationService localizationService) : base(localizationService)
    {
    }

    // GET: User/Profile
    public IActionResult Profile()
    {
        // TODO: Add authentication check
        // if (!User.Identity.IsAuthenticated)
        // {
        //     return RedirectToAction("SignIn", "Auth");
        // }
        
        return View();
    }

    // GET: User/Orders
    public IActionResult Orders()
    {
        // TODO: Add authentication check and load user orders
        // if (!User.Identity.IsAuthenticated)
        // {
        //     return RedirectToAction("SignIn", "Auth");
        // }
        
        return View();
    }

    // GET: User/Settings
    public IActionResult Settings()
    {
        // TODO: Add authentication check
        // if (!User.Identity.IsAuthenticated)
        // {
        //     return RedirectToAction("SignIn", "Auth");
        // }
        
        return View();
    }

    // POST: User/UpdateProfile
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult UpdateProfile(string fullName, string email, string phone, string address)
    {
        // TODO: Add authentication check and update profile logic
        if (ModelState.IsValid)
        {
            // Temporary update - replace with actual profile update logic
            TempData["SuccessMessage"] = "Cập nhật thông tin thành công!";
            return RedirectToAction("Profile");
        }
        
        return View("Profile");
    }

    // GET: User/Address
    public IActionResult Address()
    {
        // TODO: Add authentication check
        // if (!User.Identity.IsAuthenticated)
        // {
        //     return RedirectToAction("SignIn", "Auth");
        // }
        
        return View();
    }

    // GET: User/PaymentMethod
    public IActionResult PaymentMethod()
    {
        // TODO: Add authentication check
        // if (!User.Identity.IsAuthenticated)
        // {
        //     return RedirectToAction("SignIn", "Auth");
        // }
        
        return View();
    }
}
