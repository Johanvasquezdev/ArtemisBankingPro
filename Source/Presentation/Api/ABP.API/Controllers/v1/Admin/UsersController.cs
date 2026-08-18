using ABP.API.DTOs.User;
using ABP.Core.Application.Features.Admin.Commands;
using ABP.Core.Application.Features.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1.Admin
{
    [Route("api/v{version:apiVersion}/Admin/users")]
    [Authorize(Roles = "Admin")]
    public class UsersController(IMediator mediator) : BaseApiController
    {
        private readonly IMediator _mediator = mediator;

        // GET /api/v1/Admin/users
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? role = null)
        {
            var result = await _mediator.Send(new GetUsersQuery(page, pageSize, role));
            return Ok(result);
        }

        // GET /api/v1/Admin/users/commerce
        [HttpGet("commerce")]
        public async Task<IActionResult> GetCommerceUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new GetCommerceUsersQuery(page, pageSize));
            return Ok(result);
        }

        // GET /api/v1/Admin/users/{id}
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _mediator.Send(new GetUserByIdQuery(id));
            if (user == null) return NotFound(new { message = "El usuario especificado no existe." });
            return Ok(user);
        }

        // POST /api/v1/Admin/users
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            var adminId = User.FindFirst("uid")?.Value ?? string.Empty;

            var result = await _mediator.Send(new CreateUserCommand(
                request.FirstName, request.LastName, request.Cedula, request.UserName,
                request.Email, request.Password, request.Role, adminId, request.InitialAmount));

            if (result.CedulaAlreadyExists)
                return Conflict(new { message = "Ya existe un usuario con esta Cédula." });

            if (result.UsernameOrEmailAlreadyExists)
                return Conflict(new { message = "El nombre de usuario o correo electrónico ya se encuentra registrado." });

            return StatusCode(201, new { message = "Usuario creado exitosamente. Se envió un correo electrónico de activación." });
        }

        // POST /api/v1/Admin/users/commerce/{commerceId}
        [HttpPost("commerce/{commerceId:int}")]
        public async Task<IActionResult> CreateCommerceUser(int commerceId, [FromBody] CreateCommerceUserRequest request)
        {
            var created = await _mediator.Send(new CreateCommerceUserCommand(
                request.FirstName, request.LastName, request.Cedula, request.UserName, request.Email, request.Password, commerceId));

            if (!created)
                return Conflict(new { message = "El comercio ya tiene un usuario asociado, o el nombre de usuario/correo/cédula ya se encuentra registrado." });

            return StatusCode(201, new { message = "Usuario de comercio creado exitosamente." });
        }

        // PUT /api/v1/Admin/users/{id}
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest request)
        {
            var updated = await _mediator.Send(new UpdateUserCommand(
                id, request.FirstName, request.LastName, request.Cedula, request.Email, request.UserName,
                request.Password, request.ConfirmPassword, request.AdditionalAmount));

            if (!updated)
                return Conflict(new { message = "No se pudo actualizar el usuario. Verifique que el usuario exista y que los datos sean únicos." });

            return NoContent();
        }

        // PATCH /api/v1/Admin/users/{id}/status
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeStatus(string id, [FromBody] ChangeUserStatusRequest request)
        {
            var adminId = User.FindFirst("uid")?.Value ?? string.Empty;

            var result = await _mediator.Send(new ChangeUserStatusCommand(adminId, id, request.Status));

            if (result.SelfModificationForbidden) return Forbid();
            if (result.UserNotFound) return NotFound(new { message = "El usuario especificado no existe." });

            return NoContent();
        }
    }
}
