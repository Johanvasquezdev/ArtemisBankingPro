using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.ViewModels.Account;
using ABP.Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;

namespace ArtemisBankingPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("Admin/[controller]")]
    public class SavingsAccountManagementController(ISavingsAccountService accountService,
        IUserReadOnlyService userReadOnlyService,
        ILogger<SavingsAccountManagementController>? logger = null) : Controller
    {
        private readonly ISavingsAccountService _accountService = accountService;
        private readonly IUserReadOnlyService _userReadOnlyService = userReadOnlyService;
        private readonly ILogger<SavingsAccountManagementController> _logger = logger ?? NullLogger<SavingsAccountManagementController>.Instance;

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int page = 1, AccountStatus? status = AccountStatus.Active,
            AccountType? type = null, string? cedula = null)
        {
            var result = await _accountService.GetAllPagedAsync(page, 20, status, type, cedula);
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentType = type;
            ViewBag.CurrentCedula = cedula;
            return View(result);
        }

        [HttpGet("SelectClient")]
        public async Task<IActionResult> SelectClient(string? cedula = null)
        {
            var clients = await _userReadOnlyService.GetActiveClientsAsync(cedula);
            var vm = new SelectClientForAccountViewModel { Clients = clients, CurrentCedula = cedula };
            return View(vm);
        }

        [HttpGet("Assign/{clientId}")]
        public async Task<IActionResult> Assign(string clientId)
        {
            var hasPrimary = await _accountService.GetPrimaryAccountByClientIdAsync(clientId);
            if (hasPrimary == null)
            {
                TempData["ErrorMessage"] = "El cliente debe tener una cuenta de ahorro principal activa antes de asignarle una cuenta secundaria.";
                return RedirectToAction(nameof(SelectClient));
            }

            return View(new AssignSavingsAccountViewModel { ClientId = clientId });
        }

        [HttpPost("Assign")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignSavingsAccountViewModel model)
        {
            if (model.InitialBalance < 0)
                ModelState.AddModelError(nameof(model.InitialBalance), "El balance inicial no puede ser negativo.");

            if (!ModelState.IsValid) return View(model);

            var adminId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            var dto = new AssignSavingsAccountDto
            {
                ClientId = model.ClientId,
                AdminId = adminId,
                InitialBalance = model.InitialBalance
            };

            try
            {
                await _accountService.AssignSecondaryAsync(dto);
                TempData["SuccessMessage"] = "Cuenta de ahorro secundaria creada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al asignar una cuenta secundaria para el cliente {ClientId}", model.ClientId);
                model.HasError = true;
                model.Error = "No se pudo guardar la cuenta. Verifica la conexión y vuelve a intentarlo.";
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar una cuenta secundaria para el cliente {ClientId}", model.ClientId);
                model.HasError = true;
                model.Error = "No se pudo completar la asignación. Vuelve a intentarlo.";
                return View(model);
            }
        }

        [HttpGet("Details/{accountNumber}")]
        public async Task<IActionResult> Details(string accountNumber)
        {
            var account = await _accountService.GetByAccountNumberAsync(accountNumber);
            if (account == null) return NotFound();

            var transactions = await _accountService.GetTransactionsAsync(accountNumber);

            return View(new SavingsAccountDetailViewModel { Account = account, Transactions = transactions });
        }

        [HttpGet("Cancel/{accountNumber}")]
        public async Task<IActionResult> Cancel(string accountNumber)
        {
            var account = await _accountService.GetByAccountNumberAsync(accountNumber);
            if (account == null) return NotFound();

            if (account.Type == AccountType.Primary)
            {
                TempData["ErrorMessage"] = "Las cuentas principales no pueden ser canceladas.";
                return RedirectToAction(nameof(Index));
            }

            return View(new CancelSavingsAccountViewModel { AccountNumber = account.AccountNumber, Balance = account.Balance });
        }

        [HttpPost("Cancel/{accountNumber}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(string accountNumber)
        {
            try
            {
                await _accountService.CancelAsync(accountNumber);
                TempData["SuccessMessage"] = "Cuenta cancelada correctamente.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
