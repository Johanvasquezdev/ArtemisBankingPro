using ABP.API.DTOs.Loan;
using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1.Admin
{
    [Route("api/v{version:apiVersion}/Admin/loan")]
    [Authorize(Roles = "Admin")]
    public class LoanController(ILoanService loanService) : BaseApiController
    {
        private static readonly int[] AllowedTerms = { 6, 12, 24, 36, 48, 60 };
        private readonly ILoanService _loanService = loanService;

        // GET /api/v1/Admin/loan
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            [FromQuery] string status = "activos", [FromQuery] string? identification = null)
        {
            if (page <= 0 || pageSize <= 0 || pageSize > 20)
                return BadRequest(new { message = "Parámetros de paginación inválidos." });

            if (status is not ("activos" or "completados" or "todos"))
                return BadRequest(new { message = "El estado debe ser activos, completados o todos." });

            LoanStatus? parsedStatus = status switch
            {
                "activos" => LoanStatus.Active,
                "completados" => LoanStatus.Completed,
                _ => null
            };

            var result = await _loanService.GetAllPagedAsync(page, pageSize, parsedStatus, identification);
            return Ok(result);
        }

        // GET /api/v1/Admin/loan/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var loan = await _loanService.GetByIdAsync(id);
            if (loan == null) return NotFound(new { message = "El préstamo especificado no existe." });
            return Ok(loan);
        }

        // POST /api/v1/Admin/loan
        [HttpPost]
        public async Task<IActionResult> Assign([FromBody] AssignLoanApiDto request)
        {
            try
            {
                if (!AllowedTerms.Contains(request.MonthsInstallments))
                    return BadRequest(new { message = "El plazo seleccionado no es válido." });

                if (request.Amount <= 0)
                    return BadRequest(new { message = "El monto del préstamo debe ser mayor que cero." });

                if (request.AnnualRate < 0)
                    return BadRequest(new { message = "La tasa de interés anual no puede ser negativa." });

                if (await _loanService.ClientHasActiveLoanAsync(request.ClientId))
                    return BadRequest(new { message = "El cliente ya tiene un préstamo activo." });

                var (isHighRisk, averageDebt, currentDebt) = await _loanService.EvaluateRiskAsync(
                    request.ClientId, request.Amount, request.AnnualRate, request.MonthsInstallments);

                if (isHighRisk && !request.ConfirmHighRisk)
                {
                    return Conflict(new
                    {
                        message = "Asignar este préstamo convertirá al cliente en alto riesgo, ya que su deuda superará el promedio del sistema.",
                        riskType = currentDebt > averageDebt ? "CurrentHighRisk" : "ProjectedHighRisk",
                        currentDebt,
                        averageDebt
                    });
                }

                var adminId = User.FindFirst("uid")?.Value ?? string.Empty;

                var dto = new AssignLoanDto
                {
                    ClientId = request.ClientId,
                    Amount = request.Amount,
                    AnnualInterestRate = request.AnnualRate,
                    TermInMonths = request.MonthsInstallments,
                    AdminId = adminId
                };

                var created = await _loanService.AssignAsync(dto);
                return StatusCode(201, created);
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // PATCH /api/v1/Admin/loan/{id}/rate
        [HttpPatch("{id:int}/rate")]
        public async Task<IActionResult> UpdateRate(int id, [FromBody] UpdateRateRequest request)
        {
            if (request.NewRates < 0)
                return BadRequest(new { message = "La tasa de interés anual no puede ser negativa." });

            var loan = await _loanService.GetByIdAsync(id);
            if (loan == null) return NotFound(new { message = "El préstamo especificado no existe." });

            try
            {
                await _loanService.UpdateInterestRateAsync(id, request.NewRates);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
