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
            var result = await _mediator.Send(new GetSavingsAccountsQuery(page, pageSize, identification, status, type));
            return Ok(result);
        }

        // POST /api/v1/Admin/savings-account
        [HttpPost]
        public async Task<IActionResult> AssignSecondary([FromBody] AssignSavingsAccountApiDto request)
        {
            var adminId = User.FindFirst("uid")?.Value ?? string.Empty;

            var result = await _mediator.Send(new AssignSecondarySavingsAccountCommand(request.CedulaClient, request.InitialBalance, adminId));

            if (result.ClientNotFound)
                return NotFound(new { message = "No se encontró ningún cliente activo con esta Cédula." });

            if (result.ClientHasNoPrimaryAccount)
                return BadRequest(new { message = "El cliente debe tener una cuenta principal activa antes de poder asignarle una cuenta secundaria." });

            return StatusCode(201, new { message = "Cuenta secundaria creada exitosamente." });
        }

        // GET /api/v1/Admin/savings-account/{accountNumber}/transactions
        [HttpGet("{accountNumber}/transactions")]
        public async Task<IActionResult> GetTransactions(string accountNumber, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            var result = await _mediator.Send(new GetSavingsAccountTransactionsQuery(accountNumber, page, pageSize));
            if (result == null) return NotFound(new { message = "La cuenta especificada no existe." });

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
                return BadRequest(new { message = ex.Message });
            }
            catch (Exception)
            {
                return NotFound(new { message = "La cuenta especificada no existe." });
            }
        }
    }
}
