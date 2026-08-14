using ABP.API.DTOs.User;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1.Admin
{
    [Route("api/v{version:apiVersion}/users")]
    [Authorize(Roles = "Admin")]
    public class UsersController(IUserService userService, IUserReadOnlyService userReadOnlyService) : BaseApiController
    {
        private readonly IUserService _userService = userService;
        private readonly IUserReadOnlyService _userReadOnlyService = userReadOnlyService;

        // GET /api/v1/users
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? role = null)
        {
            if (page <= 0 || pageSize <= 0 || pageSize > 20)
                return BadRequest(new { message = "Parámetros de paginación inválidos." });

            UserRole? parsedRole = null;
            if (!string.IsNullOrWhiteSpace(role))
            {
                if (!Enum.TryParse<UserRole>(role, true, out var r) || r == UserRole.Commerce)
                    return BadRequest(new { message = "Filtro de rol inválido." });
                parsedRole = r;
            }

            var result = await _userReadOnlyService.GetAllAsync(page, pageSize, parsedRole);
            return Ok(result);
        }

        // GET /api/v1/users/commerce
        [HttpGet("commerce")]
        public async Task<IActionResult> GetCommerceUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page <= 0 || pageSize <= 0 || pageSize > 20)
                return BadRequest(new { message = "Parámetros de paginación inválidos." });

            var result = await _userReadOnlyService.GetCommerceUsersAsync(page, pageSize);
            return Ok(result);
        }

        // GET /api/v1/users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _userReadOnlyService.GetByIdAsync(id);
            if (user == null) return NotFound(new { message = "El usuario especificado no existe." });
            return Ok(user);
        }

        // POST /api/v1/users
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            if (!Enum.TryParse<UserRole>(request.Role, true, out var role) || role == UserRole.Commerce)
                return BadRequest(new { message = "El rol debe ser Administrador, Cajero o Cliente." });

            if (await _userReadOnlyService.ExistsByCedulaAsync(request.Cedula))
                return Conflict(new { message = "Ya existe un usuario con esta Cédula." });

            var adminId = User.FindFirst("uid")?.Value ?? string.Empty;

            var created = await _userService.RegisterAsync(
                request.FirstName, request.LastName, request.Cedula, request.UserName,
                request.Email, request.Password, role.ToString(), adminId, request.InitialAmount);

            if (!created)
                return Conflict(new { message = "El nombre de usuario o correo electrónico ya se encuentra registrado." });

            return StatusCode(201, new { message = "Usuario creado exitosamente. Se envió un correo electrónico de activación." });
        }

        // POST /api/v1/users/commerce/{commerceId}
        [HttpPost("commerce/{commerceId:int}")]
        public async Task<IActionResult> CreateCommerceUser(int commerceId, [FromBody] CreateCommerceUserRequest request)
        {
            var created = await _userService.RegisterCommerceUserAsync(
                request.FirstName, request.LastName, request.Cedula, request.UserName, request.Email, request.Password, commerceId);

            if (!created)
                return Conflict(new { message = "El comercio ya tiene un usuario asociado, o el nombre de usuario/correo/cédula ya se encuentra registrado." });

            return StatusCode(201, new { message = "Usuario de comercio creado exitosamente." });
        }

        // PUT /api/v1/users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest request)
        {
            var dto = new Core.Application.DTOs.User.UpdateUserDto
            {
                Id = id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Cedula = request.Cedula,
                Email = request.Email,
                Username = request.UserName,
                Password = request.Password,
                ConfirmPassword = request.ConfirmPassword,
                AdditionalAmount = request.AdditionalAmount
            };

            var updated = await _userService.UpdateAsync(dto);
            if (!updated) return Conflict(new { message = "No se pudo actualizar el usuario. Verifique que el usuario exista y que los datos sean únicos." });

            return NoContent();
        }

        // PATCH /api/v1/users/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeStatus(string id, [FromBody] ChangeUserStatusRequest request)
        {
            var adminId = User.FindFirst("uid")?.Value ?? string.Empty;

            if (adminId == id)
                return Forbid();

            var changed = await _userService.ChangeStatusAsync(adminId, id, request.Status);
            if (!changed) return NotFound(new { message = "El usuario especificado no existe." });

            return NoContent();
        }
    }
}
