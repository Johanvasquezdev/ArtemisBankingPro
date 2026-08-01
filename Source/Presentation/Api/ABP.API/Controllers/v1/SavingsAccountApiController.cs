using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using ABP.API.DTOs.SavingsAccount;
using Asp.Versioning;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Controllers.v1
{
    [ApiVersion("1.0")]
    [Authorize(Roles = nameof(UserRole.Admin))]
    public class SavingsAccountApiController(ISavingsAccountService savingsAccountService) : BaseApiController
    {
        private readonly ISavingsAccountService _savingsAccountService = savingsAccountService;

        /// <summary>
        /// Get paginated list of savings accounts
        /// </summary>
        [HttpGet]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetAll( [FromQuery] int page = 1,  [FromQuery] int pageSize = 20, [FromQuery] AccountStatus? status = null,
            [FromQuery] AccountType? type = null, [FromQuery] string? cedula = null)
        {

            if (!string.IsNullOrEmpty(status.ToString()) && Enum.TryParse<AccountStatus>(status.ToString(), out var parsedStatus))
                status = parsedStatus;

            if (!string.IsNullOrEmpty(type.ToString()) && Enum.TryParse<AccountType>(type.ToString(), out var parsedType))
                type = parsedType;
            
            if (page < 1 || pageSize < 1 || pageSize > 100)
            {
                return BadRequest(new { message = "Invalid pagination parameters." });
            }
            var result = await _savingsAccountService.GetAllPagedAsync(page, 20, status, type, cedula);
            return Ok(result);
        }

        /// <summary>
        /// Get savings account by ID
        /// </summary>
        [HttpGet("{id}")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetById(int id)
        {
            var account = await _savingsAccountService.GetByIdAsync(id);
            if (account == null)
            {
                return NotFound(new { message = "Savings account not found." });
            }
            return Ok(account);
        }


        /// <summary>
        /// Assign a secondary savings account to a client
        /// </summary>
        [HttpPost]
        [ProducesResponseType(StatusCodes.Status201Created)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status409Conflict)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Assign([FromBody] AssignSavingsAccountApiDto dto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            await _savingsAccountService.AssignSecondaryAsync(new AssignSavingsAccountDto
            {
                ClientId = dto.CedulaClient,
                InitialBalance = dto.InitialBalance
            });

            return StatusCode(201, new { message = "Account assigned correctly." });
        }

        /// <summary>
        /// Get transactions for a specific account
        /// </summary>
        [HttpGet("{accountNumber}/transactions")]
        [ProducesResponseType(StatusCodes.Status200OK)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> GetTransactions(string accountNumber)
        {
            var account = await _savingsAccountService.GetByAccountNumberAsync(accountNumber);
            if (account == null)
            {
                return NotFound(new { message = "Savings account not found." });
            }

            var transactions = await _savingsAccountService.GetTransactionsAsync(accountNumber);
            return Ok(transactions);
        }

        /// <summary>
        /// Cancel a secondary savings account
        /// </summary>
        [HttpPatch("{accountNumber}/cancel")]
        [ProducesResponseType(StatusCodes.Status204NoContent)]
        [ProducesResponseType(StatusCodes.Status400BadRequest)]
        [ProducesResponseType(StatusCodes.Status404NotFound)]
        [ProducesResponseType(StatusCodes.Status401Unauthorized)]
        [ProducesResponseType(StatusCodes.Status403Forbidden)]
        public async Task<IActionResult> Cancel(string accountNumber)
        {
            var account = await _savingsAccountService.GetByAccountNumberAsync(accountNumber);
            if (account == null)
            {
                return NotFound(new { message = "Savings account not found." });
            }

            // Cannot cancel primary accounts
            if (account.Type == AccountType.Primary)
            {
                return BadRequest(new { message = "Cannot cancel primary accounts." });
            }

            await _savingsAccountService.CancelAsync(accountNumber);
            return NoContent();
        }
    }
}
