using ABP.Core.Application.Features.Account.Commands;
using ABP.Core.Application.ViewModels.User;
using ABP.Core.Domain.Enums;
using Microsoft.AspNetCore.Mvc;
using MediatR;

namespace ArtemisBankingPro.Controllers
{
    public class AccountController : Controller
    {
        private readonly IMediator _mediator;

        public AccountController(IMediator mediator)
        {
            _mediator = mediator;
        }

        [HttpGet]
        public IActionResult Register()
        {
            if (User.Identity != null && User.Identity.IsAuthenticated)
            {
                return RedirectToAction("Index", "Home");
            }
            return View(new SaveUserViewModel());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(SaveUserViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            model.Role = UserRole.Client; // Force self-registration to be Client
            
            var result = await _mediator.Send(new RegisterAccountCommand(
                model.FirstName,
                model.LastName,
                model.Cedula,
                model.Username,
                model.Email,
                model.Password,
                model.Role.ToString(),
                "System",
                0));

            if (result)
            {
                TempData["SuccessMessage"] = "Tu cuenta ha sido creada exitosamente. Te hemos enviado un correo de confirmación. Por favor, verifica tu bandeja de entrada o la carpeta de spam para activar tu cuenta antes de iniciar sesión.";
                return RedirectToAction("Index", "Login");
            }

            model.HasError = true;
            model.Error = "Ha ocurrido un error al crear la cuenta. Verifica que el nombre de usuario, correo o cédula no estén ya registrados.";
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> Activate(string token)
        {
            if (string.IsNullOrEmpty(token))
            {
                TempData["ErrorMessage"] = "El token de activación es inválido.";
                return RedirectToAction("Index", "Login");
            }

            var result = await _mediator.Send(new ActivateAccountCommand(token));

            if (result)
            {
                TempData["SuccessMessage"] = "Tu cuenta ha sido activada correctamente. Ahora puedes iniciar sesión.";
            }
            else
            {
                TempData["ErrorMessage"] = "El enlace de activación es inválido o ha expirado.";
            }

            return RedirectToAction("Index", "Login");
        }
    }
}
