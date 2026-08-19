using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Features.Admin.Commands;
using ABP.Core.Application.Features.Admin.Queries;
using ABP.Core.Application.ViewModels.User;
using ABP.Core.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArtemisBankingPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("Admin/[controller]")]
    public class UserManagementController(IMediator mediator) : Controller
    {
        private readonly IMediator _mediator = mediator;

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int page = 1, UserRole? role = null)
        {
            var result = await _mediator.Send(new GetAdminUsersQuery(page, 20, role));
            ViewBag.CurrentRole = role;
            return View(result);
        }

        [HttpGet("Create")]
        public IActionResult Create()
        {
            return View(new SaveUserViewModel());
        }

        [HttpPost("Create")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(SaveUserViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            if (await _mediator.Send(new CheckUserCedulaQuery(model.Cedula)))
            {
                model.HasError = true;
                model.Error = "Ya existe un usuario registrado con esta cédula.";
                return View(model);
            }

            var adminId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            var created = await _mediator.Send(new CreateUserCommand(
                model.FirstName, model.LastName, model.Cedula, model.Username,
                model.Email, model.Password, model.Role.ToString(), adminId, model.InitialAmount ?? 0,
                ABP.Core.Application.DTOs.Account.AccountEmailChannel.Web));

            if (created.CedulaAlreadyExists || created.UsernameOrEmailAlreadyExists)
            {
                model.HasError = true;
                model.Error = "Ya existe un usuario registrado con este nombre de usuario o correo electrónico.";
                return View(model);
            }

            TempData["SuccessMessage"] = "Usuario creado correctamente. Se envió un correo de activación.";
            return RedirectToAction(nameof(Index));
        }

        [HttpGet("Edit/{id}")]
        public async Task<IActionResult> Edit(string id)
        {
            var adminId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (adminId == id)
            {
                TempData["ErrorMessage"] = "No puede editar su propia cuenta desde este módulo.";
                return RedirectToAction(nameof(Index));
            }

            var user = await _mediator.Send(new GetAdminUserQuery(id));
            if (user == null) return NotFound();

            var vm = new EditUserViewModel
            {
                Id = user.Id,
                FirstName = user.FirstName,
                LastName = user.LastName,
                Cedula = user.Cedula,
                Email = user.Email,
                Username = user.UserName,
                Role = user.Role
            };

            return View(vm);
        }

        [HttpPost("Edit/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(string id, EditUserViewModel model)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            bool updated;
            try
            {
                updated = await _mediator.Send(new UpdateUserCommand(
                    model.Id, model.FirstName, model.LastName, model.Cedula, model.Email, model.Username,
                    model.Password, model.ConfirmPassword, model.AdditionalAmount));
            }
            catch (InvalidOperationException exception)
            {
                model.HasError = true;
                model.Error = exception.Message;
                return View(model);
            }

            if (!updated)
            {
                model.HasError = true;
                model.Error = "No fue posible actualizar el usuario. Verifique que los datos no estén duplicados y vuelva a intentarlo.";
                return View(model);
            }

            TempData["SuccessMessage"] = "Usuario actualizado correctamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost("ChangeStatus/{id}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ChangeStatus(string id, bool isActive)
        {
            var adminId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            if (adminId == id)
            {
                TempData["ErrorMessage"] = "No puede modificar el estado de su propia cuenta.";
                return RedirectToAction(nameof(Index));
            }

            var changed = await _mediator.Send(new ChangeUserStatusCommand(adminId, id, isActive));
            TempData[changed.Success ? "SuccessMessage" : "ErrorMessage"] =
                changed.Success ? "Estado del usuario actualizado correctamente." : "No fue posible actualizar el estado del usuario.";

            return RedirectToAction(nameof(Index));
        }
    }
}
