using Microsoft.AspNetCore.Mvc;
using WebApp.Services;

namespace WebApp.Controllers
{
    [Route("auth")]
    public class AuthViewController : BaseController
    {
        public AuthViewController(IJsonLocalizationService localizationService) : base(localizationService) {}

        [HttpGet("signup")]
        public IActionResult SignUp()
        {
            return View("~/Views/Auth/SignUp.cshtml");
        }

        [HttpGet("signin")]
        public IActionResult SignIn()
        {
            return View("~/Views/Auth/SignIn.cshtml");
        }

        [HttpGet("verify")]
        public IActionResult Verify()
        {
            return View("~/Views/Auth/Verify.cshtml");
        }

        [HttpGet("forgot-password")]
        public IActionResult ForgotPassword()
        {
            return View("~/Views/Auth/ForgotPassword.cshtml");
        }

        [HttpGet("reset-password")]
        public IActionResult ResetPassword()
        {
            return View("~/Views/Auth/ResetPassword.cshtml");
        }
    }
}
