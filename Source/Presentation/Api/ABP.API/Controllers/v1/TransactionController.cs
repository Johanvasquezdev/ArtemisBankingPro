using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.DTOs.Cashier;
using ABP.Core.Application.Interfaces.IServices;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize(Roles = "Cashier")]
    public class TransactionController : BaseApiController
    {
        private readonly ITransactionService _transactionService;
        private readonly IDashboardService _dashboardService;

        public TransactionController(ITransactionService transactionService, IDashboardService dashboardService)
        {
            _transactionService = transactionService;
            _dashboardService = dashboardService;
        }

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
                return Unauthorized(new { message = "User not found in token." });
            }

            var result = await _dashboardService.GetCashierDashboardAsync(userId);
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
                return BadRequest(ModelState);

            try
            {
                await _transactionService.DepositAsync(request);
                return Ok(new { message = "Depósito completado exitosamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
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
                return BadRequest(ModelState);

            try
            {
                await _transactionService.WithdrawAsync(request);
                return Ok(new { message = "Retiro completado exitosamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
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
                return BadRequest(ModelState);

            try
            {
                await _transactionService.CashierPayCreditCardAsync(request);
                return Ok(new { message = "Pago de tarjeta de crédito completado exitosamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
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
                return BadRequest(ModelState);

            try
            {
                await _transactionService.CashierPayLoanAsync(request);
                return Ok(new { message = "Pago de préstamo completado exitosamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
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
                return BadRequest(ModelState);

            try
            {
                await _transactionService.CashierTransferAsync(request);
                return Ok(new { message = "Transferencia completada exitosamente." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
