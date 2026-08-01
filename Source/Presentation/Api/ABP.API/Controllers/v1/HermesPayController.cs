using ABP.Core.Application.DTOs.Payment;
using ABP.Core.Application.Interfaces.IServices;
using ABP.API.DTOs.Payment;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ABP.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize(Roles = "Admin,Commerce")]
    [Route("api/v{version:apiVersion}/pay")]
    public class HermesPayController : BaseApiController
    {
        private readonly IPaymentProcessorService _paymentProcessorService;

        public HermesPayController(IPaymentProcessorService paymentProcessorService)
        {
            _paymentProcessorService = paymentProcessorService;
        }

        /// <summary>
        /// Obtiene las transacciones de un comercio.
        /// Si el usuario es de tipo Commerce, usa el ID del token. Si es Admin, usa el ID de la ruta.
        /// </summary>
        /// <param name="commerceId">ID del comercio (ignorado si el usuario es Commerce)</param>
        /// <response code="200">Listado retornado exitosamente</response>
        /// <response code="401">Token ausente o inválido</response>
        [HttpGet("get-transactions/{commerceId}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetTransactions([FromRoute] int commerceId)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var actualCommerceId = commerceId;

            if (userRole == "Commerce")
            {
                var commerceIdClaim = User.FindFirst("commerceId")?.Value;
                if (!string.IsNullOrEmpty(commerceIdClaim) && int.TryParse(commerceIdClaim, out var parsedCommerceId))
                {
                    actualCommerceId = parsedCommerceId;
                }
            }

            var transactions = await _paymentProcessorService.GetCommerceTransactionsAsync(actualCommerceId);

            return Ok(transactions);
        }

        /// <summary>
        /// Procesa un pago de un comercio mediante tarjeta de crédito.
        /// Si el usuario es de tipo Commerce, usa el ID del token. Si es Admin, usa el ID de la ruta.
        /// </summary>
        /// <param name="commerceId">ID del comercio (ignorado si el usuario es Commerce)</param>
        /// <param name="request">Datos del pago a procesar</param>
        /// <response code="204">Pago procesado exitosamente</response>
        /// <response code="400">Datos inválidos o comercio/tarjeta inactiva</response>
        /// <response code="401">Token ausente o inválido</response>
        [HttpPost("process-payment/{commerceId}")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ProcessPayment(
            [FromRoute] int commerceId,
            [FromBody] ProcessPaymentRequest request)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var actualCommerceId = commerceId;

            if (userRole == "Commerce")
            {
                var commerceIdClaim = User.FindFirst("commerceId")?.Value;
                if (!string.IsNullOrEmpty(commerceIdClaim) && int.TryParse(commerceIdClaim, out var parsedCommerceId))
                {
                    actualCommerceId = parsedCommerceId;
                }
            }

            var paymentDto = new ProcessPaymentDto
            {
                CardNumber = request.CardNumber,
                MonthExpirationCard = request.MonthExpirationCard,
                YearExpirationCard = request.YearExpirationCard,
                CVC = request.CVC,
                TransactionAmount = request.TransactionAmount
            };

            var result = await _paymentProcessorService.ProcessPaymentAsync(actualCommerceId, paymentDto);

            if (!result.Success)
                return BadRequest(new { message = result.Message });

            return NoContent();
        }
    }
}
