using Microsoft.AspNetCore.Http;
using ABP.API.DTOs.CreditCard;
using ABP.Core.Application.Features.Admin.Queries;
using ABP.Core.Application.Features.Admin.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1.Admin
{
    [Route("api/v{version:apiVersion}/credit-card")]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public class CreditCardController(IMediator mediator) : BaseApiController
    {
        private readonly IMediator _mediator = mediator;

        /// <summary>
        /// Operation: GET /api/v1/credit-card
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación GET en la ruta /api/v1/credit-card.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            [FromQuery] string status = "activa", [FromQuery] string? identification = null)
        {
            if (page < 1 || pageSize is < 1 or > 20 || status is not ("activa" or "cancelada" or "todas"))
                return ApiProblem(StatusCodes.Status400BadRequest, "Validación fallida", "Los parámetros de paginación o estado no son válidos.");
            var result = await _mediator.Send(new GetCreditCardsQuery(page, pageSize, status, identification));
            return Ok(result);
        }

        /// <summary>
        /// Operation: POST /api/v1/credit-card
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación POST en la ruta /api/v1/credit-card.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Assign([FromBody] AssignCreditCardApiDto request)
        {
            try
            {
                var adminId = User?.FindFirst("uid")?.Value ?? string.Empty;
                var created = await _mediator.Send(new AssignCreditCardCommand(request.ClientId, request.CreditLimit, adminId));
                return StatusCode(201, created);
            }
            catch (InvalidOperationException ex)
            {
                return ApiProblem(StatusCodes.Status400BadRequest, "No se pudo asignar la tarjeta", ex.Message);
            }
        }

        /// <summary>
        /// Operation: GET /api/v1/credit-card/{id}
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación GET en la ruta /api/v1/credit-card/{id}.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var result = await _mediator.Send(new GetCreditCardByIdQuery(id));
            if (result == null) return ApiProblem(StatusCodes.Status404NotFound, "Tarjeta no encontrada", "La tarjeta de crédito especificada no existe.");
            return Ok(new { card = result.Card, consumptions = result.Consumptions });
        }

        /// <summary>
        /// Operation: PATCH /api/v1/credit-card/{id}/limit
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación PATCH en la ruta /api/v1/credit-card/{id}/limit.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPatch("{id:int}/limit")]
        public async Task<IActionResult> UpdateLimit(int id, [FromBody] UpdateLimitRequest request)
        {
            if (id <= 0 || request.NewLimit <= 0)
                return ApiProblem(StatusCodes.Status400BadRequest, "Límite inválido", "El límite de crédito debe ser mayor que cero.");
            try
            {
                await _mediator.Send(new UpdateCreditCardLimitCommand(id, request.NewLimit));
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return ApiProblem(StatusCodes.Status400BadRequest, "No se pudo actualizar el límite", ex.Message);
            }
            catch (Exception)
            {
                return ApiProblem(StatusCodes.Status404NotFound, "Tarjeta no encontrada", "La tarjeta de crédito especificada no existe.");
            }
        }

        /// <summary>
        /// Operation: PATCH /api/v1/credit-card/{id}/cancel
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación PATCH en la ruta /api/v1/credit-card/{id}/cancel.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPatch("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                await _mediator.Send(new CancelCreditCardCommand(id));
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return ApiProblem(StatusCodes.Status400BadRequest, "No se pudo cancelar la tarjeta", ex.Message);
            }
            catch (Exception)
            {
                return ApiProblem(StatusCodes.Status404NotFound, "Tarjeta no encontrada", "La tarjeta de crédito especificada no existe.");
            }
        }
    }
}
