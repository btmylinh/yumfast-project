using Microsoft.AspNetCore.Mvc;
using WebApp.Models;
using WebApp.Services;

namespace WebApp.Controllers;

public class AuthController : BaseController
{
    public AuthController(IJsonLocalizationService localizationService) : base(localizationService)
    {
    }
    // GET: Auth/SignIn
    public IActionResult SignIn()
    {
        // Set ViewBag values for localization
         
        return View();
    }

    // POST: Auth/SignIn
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SignIn(string email, string password)
    {
        // TODO: Implement authentication logic
        if (ModelState.IsValid)
        {
            // Temporary authentication check - replace with actual authentication
            if (!string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
            {
                // Successful login - redirect to dashboard
                return RedirectToAction("Index", "Dashboard");
            }
            else
            {
                ModelState.AddModelError("", "Email và mật khẩu không được để trống");
            }
        }

        return View();
    }

    // GET: Auth/SignUp
    public IActionResult SignUp()
    {
       
        
        return View();
    }

    // POST: Auth/SignUp
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult SignUp(string fullName, string email, string password, string confirmPassword)
    {
        // TODO: Implement user registration logic
        if (ModelState.IsValid)
        {
            if (password != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu xác nhận không khớp");
                return View();
            }

            // Temporary registration - replace with actual user creation
            if (!string.IsNullOrEmpty(fullName) && !string.IsNullOrEmpty(email) && !string.IsNullOrEmpty(password))
            {
                // Successful registration - redirect to signin
                TempData["SuccessMessage"] = "Đăng ký thành công! Vui lòng đăng nhập.";
                return RedirectToAction("SignIn");
            }
            else
            {
                ModelState.AddModelError("", "Vui lòng điền đầy đủ thông tin");
            }
        }

        return View();
    }

    // GET: Auth/ForgotPassword
    public IActionResult ForgotPassword()
    {
        return View();
    }

    // POST: Auth/ForgotPassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ForgotPassword(string email)
    {
        // TODO: Implement forgot password logic
        if (ModelState.IsValid)
        {
            if (!string.IsNullOrEmpty(email))
            {
                // Temporary forgot password - replace with actual email sending logic
                TempData["SuccessMessage"] = "Liên kết đặt lại mật khẩu đã được gửi đến email của bạn.";
                return RedirectToAction("SignIn");
            }
            else
            {
                ModelState.AddModelError("", "Vui lòng nhập email");
            }
        }

        return View();
    }

    // GET: Auth/Logout
    public IActionResult Logout()
    {
        // TODO: Implement logout logic
        // Clear authentication cookies/session

        TempData["SuccessMessage"] = "Đăng xuất thành công!";
        return RedirectToAction("Index", "Home");
    }
    
    

    // POST: Dashboard/ChangePassword
    [HttpPost]
    [ValidateAntiForgeryToken]
    public IActionResult ChangePassword(string currentPassword, string newPassword, string confirmPassword)
    {
        // TODO: Add authentication check and password change logic
        if (ModelState.IsValid)
        {
            if (newPassword != confirmPassword)
            {
                ModelState.AddModelError("", "Mật khẩu mới và xác nhận mật khẩu không khớp");
                return View("Settings");
            }

            // Temporary password change - replace with actual password change logic
            TempData["SuccessMessage"] = "Đổi mật khẩu thành công!";
            return RedirectToAction("Settings");
        }
        
        return View("Settings");
    }
}
