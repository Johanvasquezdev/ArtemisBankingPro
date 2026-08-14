using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.ViewModels.User;
using ABP.Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArtemisBankingPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("Admin/[controller]")]
    public class UserManagementController(IUserService userService, IUserReadOnlyService userReadOnlyService) : Controller
    {
        private readonly IUserService _userService = userService;
        private readonly IUserReadOnlyService _userReadOnlyService = userReadOnlyService;

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int page = 1, UserRole? role = null)
        {
            var result = await _userReadOnlyService.GetAllAsync(page, 20, role);
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

            if (await _userReadOnlyService.ExistsByCedulaAsync(model.Cedula))
            {
                model.HasError = true;
                model.Error = "Ya existe un usuario registrado con esta cédula.";
                return View(model);
            }

            var adminId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            var created = await _userService.RegisterAsync(
                model.FirstName, model.LastName, model.Cedula, model.Username,
                model.Email, model.Password, model.Role.ToString(), adminId, model.InitialAmount ?? 0);

            if (!created)
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

            var user = await _userReadOnlyService.GetByIdAsync(id);
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

            var dto = new UpdateUserDto
            {
                Id = model.Id,
                FirstName = model.FirstName,
                LastName = model.LastName,
                Cedula = model.Cedula,
                Email = model.Email,
                Username = model.Username,
                Password = model.Password,
                ConfirmPassword = model.ConfirmPassword,
                AdditionalAmount = model.AdditionalAmount
            };

            var updated = await _userService.UpdateAsync(dto);
            if (!updated)
            {
                model.HasError = true;
                model.Error = "No fue posible actualizar el usuario. Verifique que los datos no estén duplicados.";
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

            var changed = await _userService.ChangeStatusAsync(adminId, id, isActive);
            TempData[changed ? "SuccessMessage" : "ErrorMessage"] =
                changed ? "Estado del usuario actualizado correctamente." : "No fue posible actualizar el estado del usuario.";

            return RedirectToAction(nameof(Index));
        }
    }
}
