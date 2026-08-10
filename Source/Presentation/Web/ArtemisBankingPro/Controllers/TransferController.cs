using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Features.Client.Commands;
using ABP.Core.Application.Features.Client.Queries;
using ABP.Core.Application.ViewModels.Client;
using ArtemisBankingProApp.Extensions;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingProApp.Controllers
{
    [Authorize(Roles = "Client")]
    public class TransferController(IMediator mediator) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var options = await GetOptionsAsync();
            return View(new TransferOwnAccountsViewModel { Options = options });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Index(TransferOwnAccountsViewModel model)
        {
            var clientId = User.GetUserId();
            if (!ModelState.IsValid)
            {
                model.Options = await GetOptionsAsync();
                return View(model);
            }

            var result = await mediator.Send(new TransferOwnAccountsCommand(new TransferOwnAccountsDto
            {
                ClientId = clientId,
                SourceAccountNumber = model.SourceAccountNumber,
                DestinationAccountNumber = model.DestinationAccountNumber,
                Amount = model.Amount
            }));

            TempData["SuccessMessage"] = result.EmailNotificationFailed
                ? "Transferencia realizada exitosamente. No se pudo enviar la notificación por correo electrónico."
                : "Transferencia realizada exitosamente.";

            return RedirectToAction(nameof(Index));
        }

        private async Task<TransactionOptionsViewModel> GetOptionsAsync()
        {
            var clientId = User.GetUserId();
            return await mediator.Send(new GetTransactionOptionsQuery(clientId));
        }
    }
}
