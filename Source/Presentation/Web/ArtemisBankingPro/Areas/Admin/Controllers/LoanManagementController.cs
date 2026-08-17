using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.ViewModels.Client;
using ABP.Core.Application.ViewModels.Loan;
using ABP.Core.Domain.Enums;
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
        ILoanService loanService,
        ILoanInstallmentService installmentService,
        ILogger<LoanManagementController>? logger = null) : Controller
    {
        private static readonly int[] AllowedTerms = [6, 12, 18, 24, 30, 36, 42, 48, 54, 60];
        private readonly ILoanService _loanService = loanService;
        private readonly ILoanInstallmentService _installmentService = installmentService;
        private readonly ILogger<LoanManagementController> _logger = logger ?? NullLogger<LoanManagementController>.Instance;

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int page = 1, LoanStatus? status = LoanStatus.Active, string? cedula = null)
        {
            var result = await _loanService.GetAllPagedAsync(page, 20, status, cedula);
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentCedula = cedula;
            return View(result);
        }

        [HttpGet("SelectClient")]
        public async Task<IActionResult> SelectClient(string? cedula = null)
        {
            var clients = await _loanService.GetActiveClientsWithoutLoanAsync(cedula);
            var vm = new SelectClientViewModel
            {
                Clients = clients,
                AverageDebt = await _loanService.GetAverageDebtAsync(),
                CurrentCedula = cedula
            };
            return View(vm);
        }

        [HttpGet("Assign/{clientId}")]
        public IActionResult Assign(string clientId)
        {
            return View(new AssignLoanViewModel { ClientId = clientId });
        }

        [HttpPost("Assign")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignLoanViewModel model)
        {
            if (!AllowedTerms.Contains(model.TermInMonths))
                ModelState.AddModelError(nameof(model.TermInMonths), "El plazo seleccionado no es válido.");

            if (!ModelState.IsValid) return View(model);

            try
            {
                if (await _loanService.ClientHasActiveLoanAsync(model.ClientId))
                {
                    model.HasError = true;
                    model.Error = "Este cliente ya tiene un préstamo activo asignado.";
                    return View(model);
                }

                var (isHighRisk, averageDebt, currentDebt) = await _loanService.EvaluateRiskAsync(
                    model.ClientId, model.Amount, model.AnnualInterestRate, model.TermInMonths);

                if (isHighRisk && !model.RiskConfirmed)
                {
                    model.IsHighRisk = true;
                    model.AverageDebt = averageDebt;
                    model.CurrentDebt = currentDebt;
                    model.RiskMessage = currentDebt > averageDebt
                        ? "Este cliente se considera de alto riesgo, ya que su deuda actual supera el promedio del sistema."
                        : "Asignar este préstamo convertirá al cliente en un cliente de alto riesgo, ya que su deuda superará el umbral promedio del sistema.";
                    return View(model);
                }

                var adminId = User.FindFirstValue("uid") ?? User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
                var dto = new AssignLoanDto
                {
                    ClientId = model.ClientId,
                    Amount = model.Amount,
                    AnnualInterestRate = model.AnnualInterestRate,
                    TermInMonths = model.TermInMonths,
                    AdminId = adminId
                };

                await _loanService.AssignAsync(dto);
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
            var loan = await _loanService.GetByIdAsync(id);
            if (loan == null) return NotFound();

            var installments = await _installmentService.GetByLoanIdAsync(id);

            var vm = new LoanDetailViewModel
            {
                Loan = loan,
                Installments = installments,
                TotalPendingAmount = loan.PendingAmount,
                PaidInstallments = loan.PaidInstallments,
                TotalInstallments = loan.TotalInstallments
            };

            return View(vm);
        }

        [HttpGet("EditRate/{id:int}")]
        public async Task<IActionResult> EditRate(int id)
        {
            var loan = await _loanService.GetByIdAsync(id);
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
                await _loanService.UpdateInterestRateAsync(id, model.NewAnnualInterestRate);
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
