using Microsoft.AspNetCore.Http;
using ABP.API.DTOs.User;
using ABP.Core.Application.Features.Admin.Commands;
using ABP.Core.Application.Features.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1.Admin
{
    [Route("api/v{version:apiVersion}/Admin/users")]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public class UsersController(IMediator mediator) : BaseApiController
    {
        private readonly IMediator _mediator = mediator;

        /// <summary>
        /// Operation: GET /api/v1/Admin/users
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación GET en la ruta /api/v1/Admin/users.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string? role = null)
        {
            if (page < 1 || pageSize is < 1 or > 20)
                return ApiProblem(StatusCodes.Status400BadRequest, "Validación fallida", "Los parámetros de paginación no son válidos.");
            var result = await _mediator.Send(new GetUsersQuery(page, pageSize, role));
            return Ok(result);
        }

        /// <summary>
        /// Operation: GET /api/v1/Admin/users/commerce
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación GET en la ruta /api/v1/Admin/users/commerce.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("commerce")]
        public async Task<IActionResult> GetCommerceUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new GetCommerceUsersQuery(page, pageSize));
            return Ok(result);
        }

        /// <summary>
        /// Operation: GET /api/v1/Admin/users/{id}
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación GET en la ruta /api/v1/Admin/users/{id}.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("{id}")]
        public async Task<IActionResult> GetById(string id)
        {
            var user = await _mediator.Send(new GetUserByIdQuery(id));
            if (user == null) return ApiProblem(StatusCodes.Status404NotFound, "Usuario no encontrado", "El usuario especificado no existe.");
            return Ok(user);
        }

        /// <summary>
        /// Operation: POST /api/v1/Admin/users
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación POST en la ruta /api/v1/Admin/users.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateUserRequest request)
        {
            if (string.Equals(request.Role, "Commerce", StringComparison.OrdinalIgnoreCase))
                return ApiProblem(StatusCodes.Status400BadRequest, "Rol inválido", "Los usuarios de comercio deben crearse mediante el endpoint de comercio.");
            var adminId = User.FindFirst("uid")?.Value ?? string.Empty;

            var result = await _mediator.Send(new CreateUserCommand(
                request.FirstName, request.LastName, request.Cedula, request.UserName,
                request.Email, request.Password, request.Role, adminId, request.InitialAmount));

            if (result.CedulaAlreadyExists)
                return ApiProblem(StatusCodes.Status409Conflict, "Cédula duplicada", "Ya existe un usuario con esta Cédula.");

            if (result.UsernameOrEmailAlreadyExists)
                return ApiProblem(StatusCodes.Status409Conflict, "Usuario duplicado", "El nombre de usuario o correo electrónico ya se encuentra registrado.");

            return StatusCode(201, new { message = "Usuario creado exitosamente. Se envió un correo electrónico de activación." });
        }

        /// <summary>
        /// Operation: POST /api/v1/Admin/users/commerce/{commerceId}
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación POST en la ruta /api/v1/Admin/users/commerce/{commerceId}.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost("commerce/{commerceId:int}")]
        public async Task<IActionResult> CreateCommerceUser(int commerceId, [FromBody] CreateCommerceUserRequest request)
        {
            var created = await _mediator.Send(new CreateCommerceUserCommand(
                request.FirstName, request.LastName, request.Cedula, request.UserName, request.Email, request.Password, commerceId));

            if (!created)
                return ApiProblem(StatusCodes.Status409Conflict, "Usuario de comercio no creado", "El comercio ya tiene un usuario asociado, o el nombre de usuario, correo o Cédula ya se encuentra registrado.");

            return StatusCode(201, new { message = "Usuario de comercio creado exitosamente." });
        }

        /// <summary>
        /// Operation: PUT /api/v1/Admin/users/{id}
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación PUT en la ruta /api/v1/Admin/users/{id}.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut("{id}")]
        public async Task<IActionResult> Update(string id, [FromBody] UpdateUserRequest request)
        {
            var updated = await _mediator.Send(new UpdateUserCommand(
                id, request.FirstName, request.LastName, request.Cedula, request.Email, request.UserName,
                request.Password, request.ConfirmPassword, request.AdditionalAmount));

            if (!updated)
                return ApiProblem(StatusCodes.Status409Conflict, "Usuario no actualizado", "No se pudo actualizar el usuario. Verifique que el usuario exista y que los datos sean únicos.");

            return NoContent();
        }

        /// <summary>
        /// Operation: PATCH /api/v1/Admin/users/{id}/status
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación PATCH en la ruta /api/v1/Admin/users/{id}/status.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPatch("{id}/status")]
        public async Task<IActionResult> ChangeStatus(string id, [FromBody] ChangeUserStatusRequest request)
        {
            var adminId = User.FindFirst("uid")?.Value ?? string.Empty;

            if (adminId == id)
                return ApiProblem(StatusCodes.Status403Forbidden, "Operación no permitida", "No puede modificar su propia cuenta.");

            var result = await _mediator.Send(new ChangeUserStatusCommand(adminId, id, request.Status));

            if (result.SelfModificationForbidden)
                return ApiProblem(StatusCodes.Status403Forbidden, "Operación no permitida", "No puede modificar su propia cuenta.");
            if (result.UserNotFound)
                return ApiProblem(StatusCodes.Status404NotFound, "Usuario no encontrado", "El usuario especificado no existe.");

            return NoContent();
        }
    }
}
