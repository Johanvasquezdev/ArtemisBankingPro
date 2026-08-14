using Microsoft.AspNetCore.Mvc;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.DTOs.Cashier;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Areas.Cashier.Controllers
{
    [Area("Cashier")]
    public class CashierHomeController : Controller
    {
        private readonly ITransactionService _transactionService;

        public CashierHomeController(ITransactionService transactionService)
        {
            _transactionService = transactionService;
        }

        public IActionResult Index()
        {
            return View();
        }

        [HttpGet]
        public IActionResult Deposit()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Deposit(string accountNumber, decimal amount)
        {
            try
            {
                var dto = new CashierDepositDto
                {
                    AccountId = int.Parse(accountNumber), // Simplifying for demo
                    Amount = amount
                };
                await _transactionService.DepositAsync(dto);
                TempData["SuccessMessage"] = "Depósito realizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = "Error procesando el depósito: " + ex.Message;
                return View();
            }
        }

        [HttpGet]
        public IActionResult Withdraw()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> Withdraw(string accountNumber, decimal amount)
        {
            try
            {
                var dto = new CashierWithdrawalDto
                {
                    AccountId = int.Parse(accountNumber),
                    Amount = amount
                };
                await _transactionService.WithdrawAsync(dto);
                TempData["SuccessMessage"] = "Retiro realizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = "Error procesando el retiro: " + ex.Message;
                return View();
            }
        }

        [HttpGet]
        public IActionResult PayCreditCard()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PayCreditCard(string creditCardNumber, decimal amount)
        {
            try
            {
                var dto = new CashierPayCreditCardDto
                {
                    CreditCardId = int.Parse(creditCardNumber),
                    Amount = amount
                };
                await _transactionService.CashierPayCreditCardAsync(dto);
                TempData["SuccessMessage"] = "Pago a tarjeta realizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = "Error procesando el pago: " + ex.Message;
                return View();
            }
        }

        [HttpGet]
        public IActionResult PayLoan()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> PayLoan(string loanNumber, decimal amount)
        {
            try
            {
                var dto = new CashierPayLoanDto
                {
                    LoanId = int.Parse(loanNumber),
                    Amount = amount
                };
                await _transactionService.CashierPayLoanAsync(dto);
                TempData["SuccessMessage"] = "Pago a préstamo realizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = "Error procesando el pago: " + ex.Message;
                return View();
            }
        }

        [HttpGet]
        public IActionResult TransferToThirdParty()
        {
            return View();
        }

        [HttpPost]
        public async Task<IActionResult> TransferToThirdParty(string originAccount, string destinationAccount, decimal amount)
        {
            try
            {
                var dto = new CashierTransferDto
                {
                    SourceAccountId = int.Parse(originAccount),
                    DestinationAccountId = int.Parse(destinationAccount),
                    Amount = amount
                };
                await _transactionService.CashierTransferAsync(dto);
                TempData["SuccessMessage"] = "Transferencia realizada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (System.Exception ex)
            {
                TempData["ErrorMessage"] = "Error procesando la transferencia: " + ex.Message;
                return View();
            }
        }

        public IActionResult History()
        {
            return View();
        }
    }
}
