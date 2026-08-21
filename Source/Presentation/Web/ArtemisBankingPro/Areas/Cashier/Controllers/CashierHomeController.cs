using Microsoft.AspNetCore.Mvc;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.DTOs.Cashier;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using ABP.Core.Application.ViewModels.Cashier;
using ABP.Core.Domain.Exceptions;
using System.Security.Claims;
using ABP.Core.Application.Features.Cashier.Commands;
using ABP.Core.Application.Features.Cashier.Queries;
using MediatR;

namespace ArtemisBankingPro.Areas.Cashier.Controllers
{
    [Area("Cashier")]
    [Authorize(Roles = "Cashier")]
    public class CashierHomeController : Controller
    {
        private readonly IMediator _mediator;
        private readonly ILogger<CashierHomeController> _logger;

        public CashierHomeController(IMediator mediator, ILogger<CashierHomeController> logger)
        {
            _mediator = mediator;
            _logger = logger;
        }

        private string CurrentUserId => User.FindFirstValue("uid")
            ?? User.FindFirstValue(ClaimTypes.NameIdentifier)
            ?? string.Empty;

        public async Task<IActionResult> Index()
        {
            var dashboard = await _mediator.Send(new GetCashierDashboardQuery(CurrentUserId));
            return View(new CashierDashboardViewModel
            {
                TodayTransactions = dashboard.TodayTransactions,
                TodayPayments = dashboard.TodayPayments,
                TodayDeposits = dashboard.TodayDeposits,
                TodayWithdrawals = dashboard.TodayWithdrawals,
                RecentTransactions = dashboard.RecentTransactions
            });
        }

        [HttpGet]
        public IActionResult Deposit()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Deposit(string accountNumber, decimal amount, string? idempotencyKey)
        {
            try
            {
                var dto = new CashierDepositDto
                {
                    AccountNumber = accountNumber,
                    Amount = amount,
                    PerformedByUserId = CurrentUserId,
                    IdempotencyKey = idempotencyKey ?? string.Empty
                };
                await _mediator.Send(new DepositCashierCommand(dto));
                TempData["SuccessMessage"] = "Depósito realizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (FluentValidation.ValidationException ex) { TempData["ErrorMessage"] = "Datos inválidos: " + string.Join(" ", ex.Errors.Select(e => e.ErrorMessage)); return View(); } catch (System.Exception ex) { TempData["ErrorMessage"] = "Error procesando el depósito: " + ex.Message;
                return View();
            }
        }

        [HttpGet]
        public IActionResult Withdraw()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Withdraw(string accountNumber, decimal amount, string? idempotencyKey)
        {
            try
            {
                var dto = new CashierWithdrawalDto
                {
                    AccountNumber = accountNumber,
                    Amount = amount,
                    PerformedByUserId = CurrentUserId,
                    IdempotencyKey = idempotencyKey ?? string.Empty
                };
                await _mediator.Send(new WithdrawCashierCommand(dto));
                TempData["SuccessMessage"] = "Retiro realizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (FluentValidation.ValidationException ex) { TempData["ErrorMessage"] = "Datos inválidos: " + string.Join(" ", ex.Errors.Select(e => e.ErrorMessage)); return View(); } catch (System.Exception ex) { TempData["ErrorMessage"] = "Error procesando el retiro: " + ex.Message;
                return View();
            }
        }

        [HttpGet]
        public IActionResult PayCreditCard()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayCreditCard(string sourceAccount, string creditCardNumber, decimal amount, string? idempotencyKey)
        {
            try
            {
                var dto = new CashierPayCreditCardDto
                {
                    SourceAccountNumber = sourceAccount,
                    CardNumber = creditCardNumber,
                    Amount = amount,
                    PerformedByUserId = CurrentUserId,
                    IdempotencyKey = idempotencyKey ?? string.Empty
                };
                await _mediator.Send(new PayCashierCreditCardCommand(dto));
                TempData["SuccessMessage"] = "Pago a tarjeta realizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (FluentValidation.ValidationException ex) { TempData["ErrorMessage"] = "Datos inválidos: " + string.Join(" ", ex.Errors.Select(e => e.ErrorMessage)); return View(); } catch (System.Exception ex) { TempData["ErrorMessage"] = "Error procesando el pago: " + ex.Message;
                return View();
            }
        }

        [HttpGet]
        public IActionResult PayLoan()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayLoan(string sourceAccount, string loanNumber, decimal amount, string? idempotencyKey)
        {
            try
            {
                var dto = new CashierPayLoanDto
                {
                    SourceAccountNumber = sourceAccount,
                    LoanNumber = loanNumber,
                    Amount = amount,
                    PerformedByUserId = CurrentUserId,
                    IdempotencyKey = idempotencyKey ?? string.Empty
                };
                await _mediator.Send(new PayCashierLoanCommand(dto));
                TempData["SuccessMessage"] = "Pago a préstamo realizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (FluentValidation.ValidationException ex) { TempData["ErrorMessage"] = "Datos inválidos: " + string.Join(" ", ex.Errors.Select(e => e.ErrorMessage)); return View(); } catch (System.Exception ex) { TempData["ErrorMessage"] = "Error procesando el pago: " + ex.Message;
                return View();
            }
        }

        [HttpGet]
        public IActionResult TransferToThirdParty()
        {
            return View();
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> TransferToThirdParty(string originAccount, string destinationAccount, decimal amount, string? idempotencyKey)
        {
            try
            {
                var dto = new CashierTransferDto
                {
                    SourceAccountNumber = originAccount,
                    DestinationAccountNumber = destinationAccount,
                    Amount = amount,
                    PerformedByUserId = CurrentUserId,
                    IdempotencyKey = idempotencyKey ?? string.Empty
                };
                await _mediator.Send(new TransferCashierCommand(dto));
                TempData["SuccessMessage"] = "Transferencia realizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (FluentValidation.ValidationException ex) { TempData["ErrorMessage"] = "Datos inválidos: " + string.Join(" ", ex.Errors.Select(e => e.ErrorMessage)); return View(); } catch (System.Exception ex) { TempData["ErrorMessage"] = "Error procesando la transferencia: " + ex.Message;
                return View();
            }
        }

        public async Task<IActionResult> History()
        {
            var history = await _mediator.Send(new GetCashierHistoryQuery());
            return View(history);
        }
    }
}

