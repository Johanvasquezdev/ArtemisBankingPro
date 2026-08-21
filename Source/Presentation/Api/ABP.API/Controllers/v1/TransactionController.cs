using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.DTOs.Cashier;
using ABP.Core.Application.Features.Cashier.Commands;
using ABP.Core.Application.Features.Cashier.Queries;
using Asp.Versioning;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ABP.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, Roles = "Cashier")]
    public class TransactionController : BaseApiController
    {
        private readonly IMediator _mediator;

        public TransactionController(IMediator mediator)
        {
            _mediator = mediator;
        }

        private string CurrentUserId => User?.FindFirstValue("uid")
            ?? User?.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? string.Empty;

        private string IdempotencyKey => HttpContext?.Request.Headers["Idempotency-Key"].FirstOrDefault()
            ?? Guid.NewGuid().ToString("N");

        /// <summary>
        /// Obtiene el dashboard (indicadores diarios) para el Cajero.
        /// </summary>
        [HttpGet("dashboard")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        public async Task<IActionResult> GetDashboard()
        {
            var userId = User.Claims.FirstOrDefault(c => c.Type == "uid")?.Value;
            if (string.IsNullOrEmpty(userId))
            {
                return ApiProblem(401, "Token inválido", "No se encontró el usuario en el token.");
            }

            var result = await _mediator.Send(new GetCashierDashboardQuery(userId));
            return Ok(result);
        }

        /// <summary>
        /// Realiza un depósito en una cuenta de ahorros.
        /// </summary>
        [HttpPost("deposit")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Deposit([FromBody] CashierDepositDto request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem();

            request.PerformedByUserId = CurrentUserId;
            request.IdempotencyKey = IdempotencyKey;

            try
            {
                await _mediator.Send(new DepositCashierCommand(request));
                return Ok(new { message = "Depósito completado exitosamente." });
            }
            catch (Exception)
            {
                return ApiProblem(400, "Operación rechazada", "No fue posible completar el depósito.");
            }
        }

        /// <summary>
        /// Realiza un retiro de una cuenta de ahorros.
        /// </summary>
        [HttpPost("withdraw")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Withdraw([FromBody] CashierWithdrawalDto request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem();

            request.PerformedByUserId = CurrentUserId;
            request.IdempotencyKey = IdempotencyKey;

            try
            {
                await _mediator.Send(new WithdrawCashierCommand(request));
                return Ok(new { message = "Retiro completado exitosamente." });
            }
            catch (Exception)
            {
                return ApiProblem(400, "Operación rechazada", "No fue posible completar el retiro.");
            }
        }

        /// <summary>
        /// Realiza el pago a una tarjeta de crédito.
        /// </summary>
        [HttpPost("pay-credit-card")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PayCreditCard([FromBody] CashierPayCreditCardDto request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem();

            request.PerformedByUserId = CurrentUserId;
            request.IdempotencyKey = IdempotencyKey;

            try
            {
                await _mediator.Send(new PayCashierCreditCardCommand(request));
                return Ok(new { message = "Pago de tarjeta de crédito completado exitosamente." });
            }
            catch (Exception)
            {
                return ApiProblem(400, "Operación rechazada", "No fue posible completar el pago de tarjeta.");
            }
        }

        /// <summary>
        /// Realiza el pago a un préstamo.
        /// </summary>
        [HttpPost("pay-loan")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> PayLoan([FromBody] CashierPayLoanDto request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem();

            request.PerformedByUserId = CurrentUserId;
            request.IdempotencyKey = IdempotencyKey;

            try
            {
                await _mediator.Send(new PayCashierLoanCommand(request));
                return Ok(new { message = "Pago de préstamo completado exitosamente." });
            }
            catch (Exception)
            {
                return ApiProblem(400, "Operación rechazada", "No fue posible completar el pago de préstamo.");
            }
        }

        /// <summary>
        /// Realiza una transferencia a terceros o entre cuentas.
        /// </summary>
        [HttpPost("transfer")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        public async Task<IActionResult> Transfer([FromBody] CashierTransferDto request)
        {
            if (!ModelState.IsValid)
                return ValidationProblem();

            request.PerformedByUserId = CurrentUserId;
            request.IdempotencyKey = IdempotencyKey;

            try
            {
                await _mediator.Send(new TransferCashierCommand(request));
                return Ok(new { message = "Transferencia completada exitosamente." });
            }
            catch (Exception)
            {
                return ApiProblem(400, "Operación rechazada", "No fue posible completar la transferencia.");
            }
        }
    }
}
