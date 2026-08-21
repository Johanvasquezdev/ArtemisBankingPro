using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.Features.Client.Commands;
using ABP.Core.Application.Features.Client.Queries;
using ABP.Core.Application.ViewModels.Client;
using ABP.Core.Application.ViewModels.CreditCard;
using ArtemisBankingPro.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize(Roles = "Client")]
    public class CashAdvanceController(IMediator mediator) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var options = await GetOptionsAsync();
            return View(new CashAdvanceViewModel
            {
                UserAccounts = options.Accounts,
                UserCreditCards = options.CreditCards
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(CashAdvanceViewModel model)
        {
            var clientId = User.GetUserId();
            if (!ModelState.IsValid)
            {
                var options = await GetOptionsAsync();
                model.UserAccounts = options.Accounts;
                model.UserCreditCards = options.CreditCards;
                return View(model);
            }

            try
            {
                var result = await mediator.Send(new CashAdvanceCommand(new CashAdvanceDto
                {
                    ClientId = clientId,
                    CreditCardId = model.CreditCardId,
                    SavingsAccountId = model.SavingsAccountId,
                    Amount = model.Amount,
                    IdempotencyKey = model.IdempotencyKey
                }));

                TempData["SuccessMessage"] = result.EmailNotificationFailed
                    ? "Avance de efectivo realizado exitosamente. No se pudo enviar la notificación por correo electrónico."
                    : "Avance de efectivo realizado exitosamente.";

                return RedirectToAction(nameof(Index));
            }
            catch (Exception ex)
            {
                model.HasError = true; model.Error = ex.Message;
                var options = await GetOptionsAsync();
                model.UserAccounts = options.Accounts;
                model.UserCreditCards = options.CreditCards;
                return View(model);
            }
        }

        private async Task<TransactionOptionsViewModel> GetOptionsAsync()
        {
            var clientId = User.GetUserId();
            return await mediator.Send(new GetTransactionOptionsQuery(clientId));
        }
    }
}

