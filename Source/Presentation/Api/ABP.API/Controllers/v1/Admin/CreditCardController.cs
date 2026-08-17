using ABP.API.DTOs.CreditCard;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1.Admin
{
    [Route("api/v{version:apiVersion}/Admin/credit-card")]
    [Authorize(Roles = "Admin")]
    public class CreditCardController(ICreditCardService creditCardService,
        ICreditCardConsumptionService consumptionService) : BaseApiController
    {
        private readonly ICreditCardService _creditCardService = creditCardService;
        private readonly ICreditCardConsumptionService _consumptionService = consumptionService;

        // GET /api/v1/Admin/credit-card
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            [FromQuery] string status = "activa", [FromQuery] string? identification = null)
        {
            if (page <= 0 || pageSize <= 0 || pageSize > 20)
                return BadRequest(new { message = "Parámetros de paginación inválidos." });

            if (status is not ("activa" or "cancelada" or "todas"))
                return BadRequest(new { message = "El estado debe ser activa, cancelada o todas." });

            CardStatus? parsedStatus = status switch
            {
                "activa" => CardStatus.Active,
                "cancelada" => CardStatus.Cancelled,
                _ => null
            };

            var result = await _creditCardService.GetAllPagedAsync(page, pageSize, parsedStatus, identification);
            return Ok(result);
        }

        // POST /api/v1/Admin/credit-card
        [HttpPost]
        public async Task<IActionResult> Assign([FromBody] AssignCreditCardApiDto request)
        {
            if (request.CreditLimit <= 0)
                return BadRequest(new { message = "El límite de crédito debe ser mayor que cero." });

            var dto = new AssignCreditCardDto
            {
                ClientId = request.ClientId,
                CreditLimit = request.CreditLimit
            };

            try
            {
                var created = await _creditCardService.AssignAsync(dto);
                return StatusCode(201, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET /api/v1/Admin/credit-card/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var card = await _creditCardService.GetByIdAsync(id);
            if (card == null) return NotFound(new { message = "La tarjeta de crédito especificada no existe." });

            var consumptions = await _consumptionService.GetByCardIdAsync(id);

            return Ok(new { card, consumptions });
        }

        // PATCH /api/v1/Admin/credit-card/{id}/limit
        [HttpPatch("{id:int}/limit")]
        public async Task<IActionResult> UpdateLimit(int id, [FromBody] UpdateLimitRequest request)
        {
            if (request.NewLimit <= 0)
                return BadRequest(new { message = "El nuevo límite debe ser mayor que cero." });

            try
            {
                await _creditCardService.UpdateLimitAsync(id, request.NewLimit);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return NotFound(new { message = "La tarjeta de crédito especificada no existe." });
            }
        }

        // PATCH /api/v1/Admin/credit-card/{id}/cancel
        [HttpPatch("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            try
            {
                await _creditCardService.CancelAsync(id);
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return NotFound(new { message = "La tarjeta de crédito especificada no existe." });
            }
        }
    }
}
