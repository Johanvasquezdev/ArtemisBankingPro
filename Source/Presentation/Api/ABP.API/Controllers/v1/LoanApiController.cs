using ABP.Core.Application.DTOs.Loan;
using ABP.API.DTOs.Loan;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [ApiController]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public class LoanApiController(ILoanService loanService, ILoanInstallmentService installmentService) : BaseApiController
    {
        private readonly ILoanService _loanService = loanService;
        private readonly ILoanInstallmentService _installmentService = installmentService;

        /// <summary>
        /// Obtiene un listado paginado de todos los préstamos con filtros opcionales.
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll( [FromQuery] int pages = 1, [FromQuery] string? status = null, [FromQuery] string? cedula = null)
        {
            LoanStatus? loanStatus = null;
            if (!string.IsNullOrEmpty(status) && Enum.TryParse<LoanStatus>(status, out var parsed))
                loanStatus = parsed;

            var result = await _loanService.GetAllPagedAsync(pages, 20, loanStatus, cedula);
            return Ok(result);
        }

        /// <summary>
        /// Asigna un nuevo préstamo a un cliente, calculando el riesgo y generando la tabla de amortización.
        /// </summary>
        /// <remarks>
        /// Realiza validaciones de deuda máxima permitida basándose en el promedio general del banco.
        /// </remarks>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Assign([FromBody] AssignLoanApiDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            var hasLoan = await _loanService.ClientHasActiveLoanAsync(dto.ClientId);
            if (hasLoan)
                return BadRequest(new { message = "This client already has an active loan." });

            var averageDebt = await _loanService.GetAverageDebtAsync();
            var currentDebt = await _loanService.GetTotalDebtByClientIdAsync(dto.ClientId);

            var monthlyRate = dto.AnnualRate / 100 / 12;
            var n = dto.MonthsInstallments;
            decimal totalDebt = monthlyRate == 0 ? dto.Amount : dto.Amount * (decimal)(Math.Pow(1 + (double)monthlyRate, n) * 
                (double)monthlyRate / (Math.Pow(1 + (double)monthlyRate, n) - 1)) * n;

            if (currentDebt > averageDebt || currentDebt + totalDebt > averageDebt)
                return Conflict(new { message = "The client is or would become a high-risk client." });

            await _loanService.AssignAsync(new AssignLoanDto
            {
                ClientId = dto.ClientId,
                Amount = dto.Amount,
                AnnualInterestRate = dto.AnnualRate,
                TermInMonths = dto.MonthsInstallments
            });

            return StatusCode(201, new { message = "Loan created and amortization table generated." });
        }

        /// <summary>
        /// Obtiene el detalle de un préstamo específico y su tabla de amortización (cuotas).
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetDetail(int id)
        {
            var loan = await _loanService.GetByIdAsync(id);
            if (loan == null) return NotFound(new { message = "Loan not found." });

            var installments = await _installmentService.GetByLoanIdAsync(id);
            if (installments == null) return NotFound(new { message = "installments not found." });
            
            return Ok(new
            {
                loanId = loan.LoanNumber,
                amortizationTable = installments.Select(i => new
                {
                    installment = i.InstallmentNumber,
                    dueDate = i.DueDate.ToString("yyyy-MM-dd"),
                    amount = i.InstallmentAmount,
                    isPaid = i.Status.ToString() == "Paid",
                    isOverdue = i.IsOverdue
                })
            });
        }

        /// <summary>
        /// Actualiza la tasa de interés de un préstamo existente.
        /// </summary>
        /// <param name="request">Nueva tasa de interés (debe ser mayor a 0).</param>
        [HttpPatch("{id}/rate")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> UpdateRate(int id, [FromBody] UpdateRateRequest request)
        {
            if (request.NewRates <= 0)
                return BadRequest(new { message = "The rate is not valid." });

            var loan = await _loanService.GetByIdAsync(id);
            if (loan == null) return NotFound(new { message = "Loan not found." });

            await _loanService.UpdateInterestRateAsync(id, request.NewRates);
            return NoContent();
        }
    }
}
