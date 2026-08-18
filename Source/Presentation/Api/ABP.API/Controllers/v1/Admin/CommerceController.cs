using ABP.API.DTOs.Commerce;
using ABP.Core.Application.Features.Admin.Queries;
using ABP.Core.Application.Features.Admin.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1.Admin
{
    [Route("api/v{version:apiVersion}/Admin/commerce")]
    [Authorize(Roles = "Admin")]
    public class CommerceController(IMediator mediator) : BaseApiController
    {
        private readonly IMediator _mediator = mediator;

        // GET /api/v1/Admin/commerce
        // Validaciones de page/pageSize/status ahora viven en GetCommercesQueryValidator (FluentValidation).
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
                Data = result.Data
            });
        }

        // GET /api/v1/Admin/commerce/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var commerce = await _mediator.Send(new GetCommerceByIdQuery(id));
            if (commerce == null) return ApiProblem(StatusCodes.Status404NotFound, "Comercio no encontrado", "El comercio especificado no existe.");
            return Ok(commerce);
        }

        // POST /api/v1/Admin/commerce
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateCommerceRequest request)
        {
            var created = await _mediator.Send(new CreateCommerceCommand(
                request.Name, request.Description, request.Logo, request.Rnc, request.Email));
            return StatusCode(201, created);
        }

        // PUT /api/v1/Admin/commerce/{id}
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] UpdateCommerceRequest request)
        {
            var updated = await _mediator.Send(new UpdateCommerceCommand(
                id, request.Name, request.Description, request.Logo, request.Rnc, request.Email));
            if (!updated) return ApiProblem(StatusCodes.Status404NotFound, "Comercio no encontrado", "El comercio especificado no existe.");
            return NoContent();
        }

        // PATCH /api/v1/Admin/commerce/{id}/status
        [HttpPatch("{id:int}/status")]
        public async Task<IActionResult> ChangeStatus(int id, [FromBody] ChangeCommerceStatusRequest request)
        {
            var changed = await _mediator.Send(new ChangeCommerceStatusCommand(id, request.Status));
            if (!changed) return ApiProblem(StatusCodes.Status404NotFound, "Comercio no encontrado", "El comercio especificado no existe.");
            return NoContent();
        }
    }
}
