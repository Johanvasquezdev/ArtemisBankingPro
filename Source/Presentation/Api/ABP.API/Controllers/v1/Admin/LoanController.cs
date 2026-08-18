using ABP.API.DTOs.Loan;
using ABP.Core.Application.Features.Admin.Queries;
using ABP.Core.Application.Features.Admin.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1.Admin
{
    [Route("api/v{version:apiVersion}/Admin/loan")]
    [Authorize(Roles = "Admin")]
    public class LoanController(IMediator mediator) : BaseApiController
    {
        private readonly IMediator _mediator = mediator;

        // GET /api/v1/Admin/loan
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string status = "activos", 
            [FromQuery] string? identification = null)
        {
            var result = await _mediator.Send(new GetLoansQuery(page, pageSize, status, identification));
            return Ok(result);
        }

        // GET /api/v1/Admin/loan/{id}
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var loan = await _mediator.Send(new GetLoanByIdQuery(id));
            if (loan == null) return NotFound(new { message = "El prestamo especificado no existe." });
            return Ok(loan);
        }

        // POST /api/v1/Admin/loan
        [HttpPost]
        public async Task<IActionResult> Assign([FromBody] AssignLoanApiDto request)
        {
            var adminId = User.FindFirst("uid")?.Value ?? string.Empty;

            try
            {
                var result = await _mediator.Send(new AssignLoanCommand(
                    request.ClientId, request.Amount, request.AnnualRate, request.MonthsInstallments,
                    adminId, request.ConfirmHighRisk));

                if (result.IsHighRiskUnconfirmed)
                {
                    return Conflict(new
                    {
                        message = result.RiskMessage,
                        riskType = result.RiskType,
                        currentDebt = result.CurrentDebt,
                        averageDebt = result.AverageDebt
                    });
                }

                return StatusCode(201, result.Loan);
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
            try
            {
                var updated = await _mediator.Send(new UpdateLoanRateCommand(id, request.NewRates));
                if (!updated) return NotFound(new { message = "El prestamo especificado no existe." });
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }
    }
}
