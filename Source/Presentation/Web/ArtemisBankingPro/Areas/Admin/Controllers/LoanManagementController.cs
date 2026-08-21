using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.Features.Admin.Commands;
using ABP.Core.Application.Features.Admin.Queries;
using ABP.Core.Application.ViewModels.Client;
using ABP.Core.Application.ViewModels.Loan;
using ABP.Core.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;
using System.Security.Claims;

namespace ArtemisBankingPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("Admin/[controller]")]
    public class LoanManagementController(
        IMediator mediator,
        ILogger<LoanManagementController>? logger = null) : Controller
    {
        private readonly IMediator _mediator = mediator;
        private readonly ILogger<LoanManagementController> _logger = logger ?? NullLogger<LoanManagementController>.Instance;

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int page = 1, LoanStatus? status = LoanStatus.Active, string? cedula = null)
        {
            var result = await _mediator.Send(new GetAdminLoansQuery(page, 20, status, cedula));
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentCedula = cedula;
            return View(result);
        }

        [HttpGet("SelectClient")]
        public async Task<IActionResult> SelectClient(string? cedula = null)
        {
            var options = await _mediator.Send(new GetAdminLoanAssignmentOptionsQuery(cedula));
            var vm = new SelectClientViewModel
            {
                Clients = options.Clients,
                AverageDebt = options.AverageDebt,
                CurrentCedula = cedula
            };
            return View(vm);
        }

        [HttpGet("Assign/{clientId}")]
        public IActionResult Assign(string clientId)
        {
            return View(new AssignLoanViewModel { ClientId = clientId });
        }

        [HttpPost("Assign/{clientId?}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignLoanViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                var adminId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
                var result = await _mediator.Send(new AssignLoanCommand(
                    model.ClientId, model.Amount, model.AnnualInterestRate, model.TermInMonths,
                    adminId, model.RiskConfirmed));

                if (result.HasActiveLoan)
                {
                    model.HasError = true;
                    model.Error = result.Message ?? "Este cliente ya tiene un préstamo activo asignado.";
                    return View(model);
                }

                if (result.IsHighRiskUnconfirmed)
                {
                    model.IsHighRisk = true;
                    model.AverageDebt = result.AverageDebt;
                    model.CurrentDebt = result.CurrentDebt;
                    model.RiskMessage = result.RiskMessage;
                    return View(model);
                }

                TempData["SuccessMessage"] = "Préstamo asignado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                model.HasError = true;
                model.Error = ex.Message;
                return View(model);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error al asignar préstamo para el cliente {ClientId}", model.ClientId);
                model.HasError = true;
                model.Error = "No se pudo completar la asignación del préstamo. Verifica que el cliente tenga una cuenta principal activa y vuelve a intentarlo.";
                return View(model);
            }
        }

        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var details = await _mediator.Send(new GetAdminLoanDetailsQuery(id));
            if (details == null) return NotFound();
            var loan = details.Loan;

            var vm = new LoanDetailViewModel
            {
                Loan = loan,
                Installments = details.Installments,
                TotalPendingAmount = loan.PendingAmount,
                PaidInstallments = loan.PaidInstallments,
                TotalInstallments = loan.TotalInstallments
            };

            return View(vm);
        }

        [HttpGet("EditRate/{id:int}")]
        public async Task<IActionResult> EditRate(int id)
        {
            var loan = await _mediator.Send(new GetAdminLoanQuery(id));
            if (loan == null) return NotFound();

            var vm = new EditLoanRateViewModel
            {
                LoanId = id,
                LoanNumber = loan.LoanNumber,
                CurrentAnnualInterestRate = loan.AnnualInterestRate,
                NewAnnualInterestRate = loan.AnnualInterestRate
            };

            return View(vm);
        }

        [HttpPost("EditRate/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditRate(int id, EditLoanRateViewModel model)
        {
            if (id != model.LoanId) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            try
            {
                await _mediator.Send(new UpdateLoanRateCommand(id, model.NewAnnualInterestRate));
                TempData["SuccessMessage"] = "Tasa de interés actualizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                model.HasError = true;
                model.Error = ex.Message;
                return View(model);
            }
        }
    }
}
