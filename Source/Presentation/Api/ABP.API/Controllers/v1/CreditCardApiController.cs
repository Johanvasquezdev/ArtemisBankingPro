using ABP.Core.Application.DTOs.CreditCard;
using ABP.API.DTOs.CreditCard;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public class CreditCardApiController( ICreditCardService creditCardService, ICreditCardConsumptionService consumptionService) : BaseApiController
    {
        private readonly ICreditCardService _creditCardService = creditCardService;
        private readonly ICreditCardConsumptionService _consumptionService = consumptionService;

        /// <summary>
        /// Obtiene un listado paginado de tarjetas de crédito con filtros por cédula y estado.
        /// </summary
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll( [FromQuery] string? cedula = null, [FromQuery] string? status = null, [FromQuery] int pages = 1)
        {
            CardStatus? cardstatus = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<CardStatus>(status, out var parsed))
                cardstatus = parsed;

            var result = await _creditCardService.GetAllPagedAsync(pages, 20, cardstatus, cedula);
            return Ok(result);
        }

        /// <summary>
        /// Asigna una nueva tarjeta de crédito a un cliente.
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Assign([FromBody] AssignCreditCardDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _creditCardService.AssignAsync(dto);
            return StatusCode(201, new { message = "Card assigned correctly." });
        }

        /// <summary>
        /// Obtiene el detalle de una tarjeta específica y su historial de consumos.
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDetail(int id)
        {
            var card = await _creditCardService.GetByIdAsync(id);
            if (card == null) return NotFound(new { message = "Card not found." });

            var consumptions = await _consumptionService.GetByCardIdAsync(id);
            return Ok(new { consumos = consumptions });
        }

        /// <summary>
        /// Actualiza el límite de crédito de una tarjeta existente.
        /// </summary>
        /// <remarks>
        /// El nuevo límite no puede ser inferior a la deuda actual acumulada en la tarjeta.
        /// </remarks>
        [HttpPatch("{id}/limit")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateLimit(int id, [FromBody] UpdateLimitRequest request)
        {
            var card = await _creditCardService.GetByIdAsync(id);
            if (card == null) return NotFound(new { message = "Card not found.." });

            if (request.NewLimit < card.AmountOwed)
                return BadRequest(new { message = "The new limit cannot be less than the current debt." });

            await _creditCardService.UpdateLimitAsync(id, request.NewLimit);
            return NoContent();
        }

        /// <summary>
        /// Cancela definitivamente una tarjeta de crédito.
        /// </summary>
        /// <remarks>
        /// La tarjeta solo puede ser cancelada si el balance de deuda es exactamente cero.
        /// </remarks>
        [HttpPatch("{id}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Cancel(int id)
        {
            var card = await _creditCardService.GetByIdAsync(id);
            if (card == null) return NotFound(new { message = "Card not found." });

            if (card.AmountOwed > 0)
                return BadRequest(new { message = "The client still has outstanding debts." });

            await _creditCardService.CancelAsync(id);
            return NoContent();
        }
    }
}
