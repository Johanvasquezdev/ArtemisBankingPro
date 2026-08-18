using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Features.Account.Commands;
using ABP.Core.Application.ViewModels.User;
using ABP.Core.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Controllers
{
    public class LoginController : Controller
    {
        private readonly IMediator _mediator;

        public LoginController(IMediator mediator)
        {
            _mediator = mediator;
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

            var result = await _mediator.Send(new LoginCommand(model.Username, model.Password));

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
            await _mediator.Send(new GeneratePasswordResetTokenCommand(model.Username));
            
            TempData["SuccessMessage"] = "Si el usuario existe, se ha enviado un enlace de recuperación.";
            return RedirectToAction("Index");
        }

        [HttpGet]
        public IActionResult ResetPassword(string username, string token)
        {
            return View(new ResetPasswordViewModel { Username = username, Token = token });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(ResetPasswordViewModel model)
        {
            if (!ModelState.IsValid)
                return View(model);

            var result = await _mediator.Send(new ResetPasswordCommand(
                model.Username, model.Token, model.NewPassword));

            if (!result)
            {
                model.HasError = true;
                model.Error = "El enlace de recuperación es inválido o ha expirado.";
                return View(model);
            }

            TempData["SuccessMessage"] = "Tu contraseña fue actualizada correctamente.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Logout()
        {
            await _mediator.Send(new LogoutCommand());
            return RedirectToAction("LoggedOut", "Login");
        }

        [HttpGet]
        public async Task<IActionResult> Reauthenticate()
        {
            await _mediator.Send(new LogoutCommand());
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
