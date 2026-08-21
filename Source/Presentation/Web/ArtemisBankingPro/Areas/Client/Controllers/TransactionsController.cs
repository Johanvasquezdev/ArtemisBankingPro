using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Features.Client.Commands;
using ABP.Core.Application.Features.Client.Queries;
using ABP.Core.Application.ViewModels.Client;
using ArtemisBankingPro.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize(Roles = "Client")]
    public class TransactionsController(IMediator mediator) : Controller
    {
        private const string ExpressSuccess = "Transacción realizada exitosamente.";
        private const string PayCardSuccess = "Pago a tarjeta realizado exitosamente.";
        private const string PayLoanSuccess = "Pago a préstamo realizado exitosamente.";
        private const string PayBeneficiarySuccess = "Transacción a beneficiario realizada exitosamente.";

        [HttpGet]
        public async Task<IActionResult> Express()
        {
            var options = await GetOptionsAsync();
            return View(new ExpressTransactionViewModel { Options = options });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Express(ExpressTransactionViewModel model)
        {
            var clientId = User.GetUserId();
            if (!ModelState.IsValid)
            {
                model.Options = await GetOptionsAsync();
                return View(model);
            }

            try
            {
                var result = await mediator.Send(new MakeExpressTransactionCommand(new MakeExpressTransactionDto
                {
                    ClientId = clientId,
                    SourceAccountNumber = model.SourceAccountNumber,
                    DestinationAccountNumber = model.DestinationAccountNumber,
                    Amount = model.Amount,
                    IdempotencyKey = model.IdempotencyKey
                }));

                SetSuccessMessage(ExpressSuccess, result.EmailNotificationFailed);
                return RedirectToAction(nameof(Express));
            }
            catch (Exception ex)
            {
                model.HasError = true; model.Error = ex.Message;
                model.Options = await GetOptionsAsync();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> PayCreditCard()
        {
            var options = await GetOptionsAsync();
            return View(new PayCreditCardViewModel { Options = options });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayCreditCard(PayCreditCardViewModel model)
        {
            var clientId = User.GetUserId();
            if (!ModelState.IsValid)
            {
                model.Options = await GetOptionsAsync();
                return View(model);
            }

            var options = await GetOptionsAsync();
            var card = options.CreditCards.FirstOrDefault(c => c.Id == model.CreditCardId);
            if (card is null)
            {
                model.HasError = true;
                model.Error = "La tarjeta de crédito seleccionada no es válida.";
                model.Options = options;
                return View(model);
            }

            try
            {
                var result = await mediator.Send(new PayCreditCardCommand(new PayCreditCardDto
                {
                    ClientId = clientId,
                    SourceAccountNumber = model.SourceAccountNumber,
                    CreditCardId = model.CreditCardId,
                    Amount = model.Amount,
                    IdempotencyKey = model.IdempotencyKey
                }));

                SetSuccessMessage(PayCardSuccess, result.EmailNotificationFailed);
                return RedirectToAction(nameof(PayCreditCard));
            }
            catch (Exception ex)
            {
                model.HasError = true; model.Error = ex.Message;
                model.Options = await GetOptionsAsync();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> PayLoan()
        {
            var options = await GetOptionsAsync();
            return View(new PayLoanViewModel { Options = options });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayLoan(PayLoanViewModel model)
        {
            var clientId = User.GetUserId();
            if (!ModelState.IsValid)
            {
                model.Options = await GetOptionsAsync();
                return View(model);
            }

            var options = await GetOptionsAsync();
            var loan = options.Loans.FirstOrDefault(l => l.Id == model.LoanId);
            if (loan is null)
            {
                model.HasError = true;
                model.Error = "El préstamo seleccionado no es válido.";
                model.Options = options;
                return View(model);
            }

            try
            {
                var result = await mediator.Send(new PayLoanCommand(new PayLoanDto
                {
                    ClientId = clientId,
                    SourceAccountNumber = model.SourceAccountNumber,
                    LoanNumber = loan.LoanNumber,
                    Amount = model.Amount,
                    IdempotencyKey = model.IdempotencyKey
                }));

                SetSuccessMessage(PayLoanSuccess, result.EmailNotificationFailed);
                return RedirectToAction(nameof(PayLoan));
            }
            catch (Exception ex)
            {
                model.HasError = true; model.Error = ex.Message;
                model.Options = await GetOptionsAsync();
                return View(model);
            }
        }

        [HttpGet]
        public async Task<IActionResult> PayBeneficiary()
        {
            var options = await GetOptionsAsync();
            return View(new PayBeneficiaryViewModel { Options = options });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> PayBeneficiary(PayBeneficiaryViewModel model)
        {
            var clientId = User.GetUserId();
            if (!ModelState.IsValid)
            {
                model.Options = await GetOptionsAsync();
                return View(model);
            }

            try
            {
                var result = await mediator.Send(new PayBeneficiaryCommand(new PayBeneficiaryDto
                {
                    ClientId = clientId,
                    BeneficiaryId = model.BeneficiaryId,
                    SourceAccountNumber = model.SourceAccountNumber,
                    Amount = model.Amount,
                    IdempotencyKey = model.IdempotencyKey
                }));

                SetSuccessMessage(PayBeneficiarySuccess, result.EmailNotificationFailed);
                return RedirectToAction(nameof(PayBeneficiary));
            }
            catch (Exception ex)
            {
                model.HasError = true; model.Error = ex.Message;
                model.Options = await GetOptionsAsync();
                return View(model);
            }
        }

        private async Task<TransactionOptionsViewModel> GetOptionsAsync()
        {
            var clientId = User.GetUserId();
            return await mediator.Send(new GetTransactionOptionsQuery(clientId));
        }

        private void SetSuccessMessage(string message, bool emailNotificationFailed)
        {
            TempData["SuccessMessage"] = emailNotificationFailed
                ? $"{message} No se pudo enviar la notificación por correo electrónico."
                : message;
        }
    }
}

