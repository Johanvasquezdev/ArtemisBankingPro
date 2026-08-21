using ABP.Core.Application.DTOs.Payment;
using ABP.API.DTOs.Payment;
using ABP.Core.Application.Features.Commerce.Commands;
using ABP.Core.Application.Features.Commerce.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using ABP.Core.Domain.Interfaces;

namespace ABP.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, Roles = "Admin,Commerce")]
    [Route("api/v{version:apiVersion}/pay")]
    public class HermesPayController : BaseApiController
    {
        private readonly IMediator _mediator;
        private readonly ICommerceRepository _commerceRepo;

        public HermesPayController(IMediator mediator, ICommerceRepository commerceRepo)
        {
            _mediator = mediator;
            _commerceRepo = commerceRepo;
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
        public async Task<IActionResult> GetTransactions([FromRoute] int commerceId, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var actualCommerceId = commerceId;

            if (userRole == "Commerce")
            {
                var commerceIdClaim = User.FindFirst("commerceId")?.Value;
                if (string.IsNullOrEmpty(commerceIdClaim) || !int.TryParse(commerceIdClaim, out var parsedCommerceId))
                {
                    return Forbid();
                }
                actualCommerceId = parsedCommerceId;
            }

            var commerce = await _commerceRepo.GetByIdAsync(actualCommerceId);
            if (commerce == null || !commerce.IsActive)
            {
                return BadRequest("El comercio no existe o está inactivo.");
            }

            var transactions = await _mediator.Send(new GetCommerceTransactionsQuery(actualCommerceId, page, pageSize));

            return Ok(new
            {
                page = transactions.Page,
                pageSize = transactions.PageSize,
                totalRecords = transactions.TotalCount,
                totalPages = transactions.TotalPages,
                data = transactions.Items
            });
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
        [ProducesResponseType(typeof(PaymentResultDto), StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> ProcessPayment(
            [FromRoute] int commerceId,
            [FromBody] ProcessPaymentRequest request,
            [FromHeader(Name = "Idempotency-Key")] string? idempotencyKey = null)
        {
            if (!ModelState.IsValid)
                return ValidationProblem();

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var actualCommerceId = commerceId;

            if (userRole == "Commerce")
            {
                var commerceIdClaim = User.FindFirst("commerceId")?.Value;
                if (string.IsNullOrEmpty(commerceIdClaim) || !int.TryParse(commerceIdClaim, out var parsedCommerceId))
                {
                    return Forbid();
                }
                actualCommerceId = parsedCommerceId;
            }

            var paymentDto = new ProcessPaymentDto
            {
                CardNumber = request.CardNumber,
                MonthExpirationCard = request.MonthExpirationCard,
                YearExpirationCard = request.YearExpirationCard,
                CVC = request.CVC,
                TransactionAmount = request.TransactionAmount,
                IdempotencyKey = idempotencyKey ?? string.Empty
            };

            var result = await _mediator.Send(new ProcessCommercePaymentCommand(actualCommerceId, paymentDto));

            if (!result.Success)
                return ApiProblem(400, "Pago rechazado", result.Message ?? "El pago no pudo procesarse.");

            return Ok(result);
        }
    }
}
