using Microsoft.AspNetCore.Http;
using ABP.API.DTOs.Loan;
using ABP.Core.Application.Features.Admin.Queries;
using ABP.Core.Application.Features.Admin.Commands;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1.Admin
{
    [Route("api/v{version:apiVersion}/loan")]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public class LoanController(IMediator mediator) : BaseApiController
    {
        private readonly IMediator _mediator = mediator;

        /// <summary>
        /// Operation: GET /api/v1/loan
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación GET en la ruta /api/v1/loan.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20, [FromQuery] string status = "activos", 
            [FromQuery] string? identification = null)
        {
            if (page < 1 || pageSize is < 1 or > 20 || status is not ("activos" or "completados" or "todos"))
                return ApiProblem(StatusCodes.Status400BadRequest, "Validación fallida", "Los parámetros de paginación o estado no son válidos.");
            var result = await _mediator.Send(new GetLoansQuery(page, pageSize, status, identification));
            return Ok(result);
        }

        /// <summary>
        /// Operation: GET /api/v1/loan/{id}
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación GET en la ruta /api/v1/loan/{id}.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var loan = await _mediator.Send(new GetLoanByIdQuery(id));
            if (loan == null) return ApiProblem(StatusCodes.Status404NotFound, "Préstamo no encontrado", "El préstamo especificado no existe.");
            return Ok(loan);
        }

        /// <summary>
        /// Operation: POST /api/v1/loan
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación POST en la ruta /api/v1/loan.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPost]
        public async Task<IActionResult> Assign([FromBody] AssignLoanApiDto request)
        {
            if (request.Amount <= 0 || request.AnnualRate < 0 || request.MonthsInstallments is not (6 or 12 or 24 or 36 or 48 or 60))
                return ApiProblem(StatusCodes.Status400BadRequest, "Datos del préstamo inválidos", "El monto, la tasa o el plazo del préstamo no son válidos.");
            var adminId = User.FindFirst("uid")?.Value ?? string.Empty;

            try
            {
                var result = await _mediator.Send(new AssignLoanCommand(
                    request.ClientId, request.Amount, request.AnnualRate, request.MonthsInstallments,
                    adminId, request.ConfirmHighRisk));

                if (result.HasActiveLoan)
                    return ApiProblem(StatusCodes.Status409Conflict, "Préstamo activo", result.Message ?? "El cliente ya tiene un préstamo activo.");

                if (result.IsHighRiskUnconfirmed)
                {
                    return ApiProblem(StatusCodes.Status409Conflict, "Confirmación de riesgo requerida",
                        result.RiskMessage ?? "El préstamo requiere confirmación por riesgo.",
                        new { riskType = result.RiskType, currentDebt = result.CurrentDebt, averageDebt = result.AverageDebt });
                }

                return StatusCode(201, result.Loan);
            }
            catch (KeyNotFoundException ex)
            {
                return ApiProblem(StatusCodes.Status404NotFound, "Cliente no encontrado", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ApiProblem(StatusCodes.Status400BadRequest, "No se pudo asignar el préstamo", ex.Message);
            }
        }

        /// <summary>
        /// Operation: PATCH /api/v1/loan/{id}/rate
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación PATCH en la ruta /api/v1/loan/{id}/rate.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
        [HttpPatch("{id:int}/rate")]
        public async Task<IActionResult> UpdateRate(int id, [FromBody] UpdateRateRequest request)
        {
            try
            {
                var updated = await _mediator.Send(new UpdateLoanRateCommand(id, request.NewRates));
                if (!updated) return ApiProblem(StatusCodes.Status404NotFound, "Préstamo no encontrado", "El préstamo especificado no existe.");
                return NoContent();
            }
            catch (Exception ex)
            {
                return ApiProblem(StatusCodes.Status400BadRequest, "No se pudo actualizar la tasa", ex.Message);
            }
        }
    }
}
