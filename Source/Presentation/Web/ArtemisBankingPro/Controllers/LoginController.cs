using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.ViewModels.User;
using ABP.Core.Domain.Enums;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Controllers
{
    public class LoginController : Controller
    {
        private readonly IUserService _userService;

        public LoginController(IUserService userService)
        {
            _userService = userService;
        }

        [HttpGet]
        public IActionResult Index()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToDashboard(User.IsInRole(UserRole.Admin.ToString()), User.IsInRole(UserRole.Cashier.ToString()));
            }
            return View(new LoginViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(LoginViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var result = await _userService.AuthenticateAsync(model.Username, model.Password);

            if (result.Success)
            {
                return RedirectToDashboard(result.Role == UserRole.Admin, result.Role == UserRole.Cashier);
            }

            model.HasError = true;
            model.Error = result.Error;
            return View(model);
        }

        [HttpGet]
        public IActionResult ForgotPassword()
        {
            return View(new ForgotPasswordViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ForgotPassword(ForgotPasswordViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Since GeneratePasswordResetTokenAsync sends an email, we just show a success message regardless of actual existence (security best practice)
            await _userService.GeneratePasswordResetTokenAsync(model.Username);
            
            TempData["SuccessMessage"] = "Si el usuario existe, se ha enviado un enlace de recuperación.";
            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Logout()
        {
            await _userService.LogoutAsync();
            return RedirectToAction("LoggedOut", "Login");
        }

        [HttpGet]
        public async Task<IActionResult> Reauthenticate()
        {
            await _userService.LogoutAsync();
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("/LoggedOut")]
        public IActionResult LoggedOut()
        {
            return View();
        }

        [HttpGet("/AccessDenied")]
        public IActionResult AccessDenied()
        {
            return View();
        }

        private IActionResult RedirectToDashboard(bool isAdmin, bool isCashier)
        {
            if (isAdmin)
            {
                return RedirectToAction("Index", "Dashboard", new { area = "Admin" });
            }
            if (isCashier)
            {
                return RedirectToAction("Index", "CashierHome", new { area = "Cashier" });
            }
            return RedirectToAction("Index", "Home");
        }
    }
}
