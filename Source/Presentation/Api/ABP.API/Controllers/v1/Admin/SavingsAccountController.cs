using Microsoft.AspNetCore.Http;
using ABP.API.DTOs.SavingsAccount;
using ABP.Core.Application.Features.Admin.Commands;
using ABP.Core.Application.Features.Admin.Queries;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1.Admin
{
    [Route("api/v{version:apiVersion}/savings-account")]
    [Authorize(AuthenticationSchemes = Microsoft.AspNetCore.Authentication.JwtBearer.JwtBearerDefaults.AuthenticationScheme, Roles = "Admin")]
    public class SavingsAccountController(IMediator mediator) : BaseApiController
    {
        private readonly IMediator _mediator = mediator;

        /// <summary>
        /// Operation: GET /api/v1/savings-account
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación GET en la ruta /api/v1/savings-account.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

        /// <summary>
        /// Operation: POST /api/v1/savings-account
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación POST en la ruta /api/v1/savings-account.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

        /// <summary>
        /// Operation: GET /api/v1/savings-account/{accountNumber}/transactions
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación GET en la ruta /api/v1/savings-account/{accountNumber}/transactions.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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

        /// <summary>
        /// Operation: PATCH /api/v1/savings-account/{accountNumber}/cancel
        /// </summary>
        /// <remarks>
        /// Ejecuta la operación PATCH en la ruta /api/v1/savings-account/{accountNumber}/cancel.
        /// </remarks>
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status500InternalServerError)]
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
