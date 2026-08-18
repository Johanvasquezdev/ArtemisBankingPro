using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.Features.Admin.Commands;
using ABP.Core.Application.Features.Admin.Queries;
using ABP.Core.Application.ViewModels.Account;
using ABP.Core.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;

namespace ArtemisBankingPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("Admin/[controller]")]
    public class SavingsAccountManagementController(IMediator mediator,
        ILogger<SavingsAccountManagementController>? logger = null) : Controller
    {
        private readonly IMediator _mediator = mediator;
        private readonly ILogger<SavingsAccountManagementController> _logger = logger ?? NullLogger<SavingsAccountManagementController>.Instance;

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int page = 1, AccountStatus? status = AccountStatus.Active,
            AccountType? type = null, string? cedula = null)
        {
            var result = await _mediator.Send(new GetAdminSavingsAccountsQuery(page, 20, status, type, cedula));
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentType = type;
            ViewBag.CurrentCedula = cedula;
            return View(result);
        }

        [HttpGet("SelectClient")]
        public async Task<IActionResult> SelectClient(string? cedula = null)
        {
            var clients = await _mediator.Send(new GetActiveClientsQuery(cedula));
            var vm = new SelectClientForAccountViewModel { Clients = clients, CurrentCedula = cedula };
            return View(vm);
        }

        [HttpGet("Assign/{clientId}")]
        public async Task<IActionResult> Assign(string clientId)
        {
            var hasPrimary = await _mediator.Send(new GetPrimarySavingsAccountQuery(clientId));
            if (hasPrimary == null)
            {
                TempData["ErrorMessage"] = "El cliente debe tener una cuenta de ahorro principal activa antes de asignarle una cuenta secundaria.";
                return RedirectToAction(nameof(SelectClient));
            }

            return View(new AssignSavingsAccountViewModel { ClientId = clientId });
        }

        [HttpPost("Assign/{clientId?}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignSavingsAccountViewModel model)
        {
            if (model.InitialBalance < 0)
                ModelState.AddModelError(nameof(model.InitialBalance), "El balance inicial no puede ser negativo.");

            if (!ModelState.IsValid) return View(model);

            var adminId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;

            try
            {
                var client = await _mediator.Send(new GetAdminUserQuery(model.ClientId));
                if (client is null)
                {
                    model.HasError = true;
                    model.Error = "No se encontró el cliente seleccionado.";
                    return View(model);
                }

                var result = await _mediator.Send(new AssignSecondarySavingsAccountCommand(
                    client.Cedula, model.InitialBalance, adminId));
                if (result.ClientNotFound || result.ClientHasNoPrimaryAccount)
                {
                    model.HasError = true;
                    model.Error = result.ClientHasNoPrimaryAccount
                        ? "El cliente debe tener una cuenta principal activa antes de poder asignarle una cuenta secundaria."
                        : "No se encontró ningún cliente activo con esta Cédula.";
                    return View(model);
                }
                TempData["SuccessMessage"] = "Cuenta de ahorro secundaria creada correctamente.";
                return RedirectToAction(nameof(Index));
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
            var details = await _mediator.Send(new GetAdminSavingsAccountTransactionsQuery(accountNumber));
            if (details == null) return NotFound();

            return View(new SavingsAccountDetailViewModel { Account = details.Account, Transactions = details.Transactions });
        }

        [HttpGet("Cancel/{accountNumber}")]
        public async Task<IActionResult> Cancel(string accountNumber)
        {
            var account = await _mediator.Send(new GetAdminSavingsAccountQuery(accountNumber));
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
                await _mediator.Send(new CancelSavingsAccountCommand(accountNumber));
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
