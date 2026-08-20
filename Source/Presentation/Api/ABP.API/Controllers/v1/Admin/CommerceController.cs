using ABP.API.DTOs.Commerce;
using ABP.Core.Application.Features.Admin.Commands;
using ABP.Core.Application.Features.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1.Admin
{
    [Route("api/v{version:apiVersion}/commerce")]
    [Authorize(AuthenticationSchemes = "Bearer", Roles = "Admin")]
    public class CommerceController(IMediator mediator) : BaseApiController
    {
        private readonly IMediator _mediator = mediator;

        /// <summary>
        /// Operation: GET /api/v1/Admin/commerce
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación GET en la ruta /api/v1/Admin/commerce.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string status = "activo")
        {
            if (page < 1 || pageSize is < 1 or > 20 || status is not ("activo" or "inactivo" or "todos"))
                return ApiProblem(StatusCodes.Status400BadRequest, "Validación fallida", "Los parámetros de paginación o estado no son válidos.");

            var result = await _mediator.Send(new GetCommercesQuery(page, pageSize, status));

            return Ok(new
            {
                result.Page,
                result.PageSize,
                result.TotalRecords,
                result.Data
            });
        }

        /// <summary>
        /// Operation: GET /api/v1/Admin/commerce/{id}
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación GET en la ruta /api/v1/Admin/commerce/{id}.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _mediator.Send(new GetCommerceByIdQuery(id));
            if (result == null)
                return ApiProblem(StatusCodes.Status404NotFound, "Comercio no encontrado", "El comercio especificado no existe.");

            return Ok(new
            {
                id = result.Commerce.Id,
                result.Commerce.Name,
                result.Commerce.Description,
                result.Commerce.Logo,
                result.Commerce.Email,
                result.Commerce.PhoneNumber,
                result.Commerce.Rnc,
                result.Commerce.IsActive,
                result.Commerce.CreatedAt,
                associatedUser = result.AssociatedUser
            });
        }

        /// <summary>
        /// Operation: POST /api/v1/Admin/commerce
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación POST en la ruta /api/v1/Admin/commerce.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCommerceRequest request)
        {
            var adminId = User?.FindFirst("uid")?.Value ?? string.Empty;

            var result = await _mediator.Send(new CreateCommerceCommand(
                request.Name, request.Description, request.Logo,
                request.Email, request.PhoneNumber, request.Rnc, adminId));

            if (result.RncAlreadyExists)
                return ApiProblem(StatusCodes.Status409Conflict, "Comercio duplicado", "Ya existe un comercio con el mismo RNC.");

            if (result.EmailAlreadyExists)
                return ApiProblem(StatusCodes.Status409Conflict, "Comercio duplicado", "Ya existe un comercio con el mismo correo electrónico.");

            return StatusCode(201, result.Commerce);
        }

        /// <summary>
        /// Operation: PUT /api/v1/Admin/commerce/{id}
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación PUT en la ruta /api/v1/Admin/commerce/{id}.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCommerceRequest request)
        {
            var result = await _mediator.Send(new UpdateCommerceCommand(
                id, request.Name, request.Description, request.Logo,
                request.Email, request.PhoneNumber, request.Rnc));

            if (result.NotFound)
                return ApiProblem(StatusCodes.Status404NotFound, "Comercio no encontrado", "El comercio especificado no existe.");
            if (result.RncAlreadyExists)
                return ApiProblem(StatusCodes.Status409Conflict, "RNC duplicado", "El RNC pertenece a otro comercio.");
            if (result.EmailAlreadyExists)
                return ApiProblem(StatusCodes.Status409Conflict, "Correo duplicado", "El correo electrónico pertenece a otro comercio.");

            return NoContent();
        }

        /// <summary>
        /// Operation: PATCH /api/v1/Admin/commerce/{id}/status
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación PATCH en la ruta /api/v1/Admin/commerce/{id}/status.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeCommerceStatusRequest request)
        {
            var changed = await _mediator.Send(new ChangeCommerceStatusCommand(id, request.Status));
            if (!changed)
                return ApiProblem(StatusCodes.Status404NotFound, "Comercio no encontrado", "El comercio especificado no existe.");
            return NoContent();
        }
    }
}
