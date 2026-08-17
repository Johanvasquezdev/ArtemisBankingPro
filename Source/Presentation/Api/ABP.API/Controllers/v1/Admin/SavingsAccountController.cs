using ABP.API.DTOs.SavingsAccount;
using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1.Admin
{
    [Route("api/v{version:apiVersion}/Admin/savings-account")]
    [Authorize(Roles = "Admin")]
    public class SavingsAccountController(ISavingsAccountService accountService,
        IUserReadOnlyService userReadOnlyService) : BaseApiController
    {
        private readonly ISavingsAccountService _accountService = accountService;
        private readonly IUserReadOnlyService _userReadOnlyService = userReadOnlyService;

        // GET /api/v1/Admin/savings-account
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int page = 1, [FromQuery] int pageSize = 20,
            [FromQuery] string? identification = null, [FromQuery] string status = "activa", [FromQuery] string type = "todas")
        {
            if (page <= 0 || pageSize <= 0 || pageSize > 20)
                return BadRequest(new { message = "Parámetros de paginación inválidos." });

            if (status is not ("activa" or "cancelada" or "todas"))
                return BadRequest(new { message = "El estado debe ser activa, cancelada o todas." });

            if (type is not ("principal" or "secundaria" or "todas"))
                return BadRequest(new { message = "El tipo debe ser principal, secundaria o todas." });

            AccountStatus? parsedStatus = status switch
            {
                "activa" => AccountStatus.Active,
                "cancelada" => AccountStatus.Closed,
                _ => null
            };

            AccountType? parsedType = type switch
            {
                "principal" => AccountType.Primary,
                "secundaria" => AccountType.Secondary,
                _ => null
            };

            var result = await _accountService.GetAllPagedAsync(page, pageSize, parsedStatus, parsedType, identification);
            return Ok(result);
        }

        // POST /api/v1/Admin/savings-account
        [HttpPost]
        public async Task<IActionResult> AssignSecondary([FromBody] AssignSavingsAccountApiDto request)
        {
            if (request.InitialBalance < 0)
                return BadRequest(new { message = "El balance inicial no puede ser negativo." });

            try
            {
                var matches = await _userReadOnlyService.GetActiveClientsAsync(request.CedulaClient);
                var client = matches.FirstOrDefault(c => c.Cedula == request.CedulaClient);

                if (client == null)
                    return NotFound(new { message = "No se encontró ningún cliente activo con esta Cédula." });

                var primaryAccount = await _accountService.GetPrimaryAccountByClientIdAsync(client.Id);
                if (primaryAccount == null)
                    return BadRequest(new { message = "El cliente debe tener una cuenta principal activa antes de poder asignarle una cuenta secundaria." });

                var adminId = User.FindFirst("uid")?.Value ?? string.Empty;

                var dto = new AssignSavingsAccountDto
                {
                    ClientId = client.Id,
                    AdminId = adminId,
                    InitialBalance = request.InitialBalance
                };

                await _accountService.AssignSecondaryAsync(dto);
                return StatusCode(201, new { message = "Cuenta secundaria creada exitosamente." });
            }
            catch (InvalidOperationException ex)
            {
                return BadRequest(new { message = ex.Message });
            }
        }

        // GET /api/v1/Admin/savings-account/{accountNumber}/transactions
        [HttpGet("{accountNumber}/transactions")]
        public async Task<IActionResult> GetTransactions(string accountNumber, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
        {
            if (page <= 0 || pageSize <= 0 || pageSize > 20)
                return BadRequest(new { message = "Parámetros de paginación inválidos." });

            var account = await _accountService.GetByAccountNumberAsync(accountNumber);
            if (account == null) return NotFound(new { message = "La cuenta especificada no existe." });

            var transactions = await _accountService.GetTransactionsAsync(accountNumber);
            var page_ = transactions.Skip((page - 1) * pageSize).Take(pageSize);

            return Ok(new
            {
                accountNumber = account.AccountNumber,
                account.Balance,
                account.Type,
                account.Status,
                transactions = new
                {
                    page,
                    pageSize,
                    totalRecords = transactions.Count(),
                    data = page_
                }
            });
        }

        // PATCH /api/v1/Admin/savings-account/{accountNumber}/cancel
        [HttpPatch("{accountNumber}/cancel")]
        public async Task<IActionResult> Cancel(string accountNumber)
        {
            try
            {
                await _accountService.CancelAsync(accountNumber);
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
