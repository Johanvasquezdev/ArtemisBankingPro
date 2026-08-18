using ABP.API.DTOs.SavingsAccount;
using ABP.Core.Application.Features.Admin.Commands;
using ABP.Core.Application.Features.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1.Admin
{
    [Route("api/v{version:apiVersion}/Admin/savings-account")]
    [Authorize(Roles = "Admin")]
    public class SavingsAccountController(IMediator mediator) : BaseApiController
    {
        private readonly IMediator _mediator = mediator;

        // GET /api/v1/Admin/savings-account
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            [FromQuery] string? identification = null, [FromQuery] string status = "activa", [FromQuery] string type = "todas")
        {
            if (page < 1 || pageSize is < 1 or > 20 || status is not ("activa" or "cancelada" or "todas")
                || type is not ("principal" or "secundaria" or "todas"))
                return ApiProblem(StatusCodes.Status400BadRequest, "Validación fallida", "Los parámetros de paginación, estado o tipo no son válidos.");
            var result = await _mediator.Send(new GetSavingsAccountsQuery(page, pageSize, identification, status, type));
            return Ok(result);
        }

        // POST /api/v1/Admin/savings-account
        [HttpPost]
        public async Task<IActionResult> AssignSecondary([FromBody] AssignSavingsAccountApiDto request)
        {
            if (request.InitialBalance < 0 || string.IsNullOrWhiteSpace(request.CedulaClient))
                return ApiProblem(StatusCodes.Status400BadRequest, "Datos de cuenta inválidos", "La Cédula y el balance inicial deben ser válidos.");
            var adminId = User.FindFirst("uid")?.Value ?? string.Empty;

            try
            {
                await _mediator.Send(new AssignSecondarySavingsAccountByCedulaCommand(
                    request.CedulaClient, request.InitialBalance, adminId));
                return StatusCode(201, new { message = "Cuenta secundaria creada exitosamente." });
            }
            catch (KeyNotFoundException ex)
            {
                return ApiProblem(StatusCodes.Status404NotFound, "Cliente no encontrado", ex.Message);
            }
            catch (InvalidOperationException ex)
            {
                return ApiProblem(StatusCodes.Status400BadRequest, "Cuenta principal requerida", ex.Message);
            }
        }

        // GET /api/v1/Admin/savings-account/{accountNumber}/transactions
        [HttpGet("{accountNumber}/transactions")]
        public async Task<IActionResult> GetTransactions(string accountNumber, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new GetSavingsAccountTransactionsQuery(accountNumber, page, pageSize));
            if (result == null) return ApiProblem(StatusCodes.Status404NotFound, "Cuenta no encontrada", "La cuenta especificada no existe.");

            return Ok(new
            {
                accountNumber = result.Account.AccountNumber,
                result.Account.Balance,
                result.Account.Type,
                result.Account.Status,
                transactions = new
                {
                    result.Page,
                    result.PageSize,
                    result.TotalRecords,
                    data = result.Data
                }
            });
        }

        // PATCH /api/v1/Admin/savings-account/{accountNumber}/cancel
        [HttpPatch("{accountNumber}/cancel")]
        public async Task<IActionResult> Cancel(string accountNumber)
        {
            try
            {
                await _mediator.Send(new CancelSavingsAccountCommand(accountNumber));
                return NoContent();
            }
            catch (InvalidOperationException ex)
            {
                return ApiProblem(StatusCodes.Status400BadRequest, "No se pudo cancelar la cuenta", ex.Message);
            }
            catch (Exception)
            {
                return ApiProblem(StatusCodes.Status404NotFound, "Cuenta no encontrada", "La cuenta especificada no existe.");
            }
        }
    }
}
