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
    public class BeneficiariesController(IMediator mediator) : Controller
    {
        [HttpGet]
        public async Task<IActionResult> Index()
        {
            var clientId = User.GetUserId();
            var beneficiaries = await mediator.Send(new GetBeneficiariesQuery(clientId));

            return View(new BeneficiariesViewModel
            {
                Beneficiaries = beneficiaries
            });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(BeneficiariesViewModel model)
        {
            var clientId = User.GetUserId();
            if (!ModelState.IsValid)
            {
                model.Beneficiaries = await mediator.Send(new GetBeneficiariesQuery(clientId));
                return View(nameof(Index), model);
            }

            await mediator.Send(new AddBeneficiaryCommand(clientId, model.Add.AccountNumber));
            TempData["SuccessMessage"] = "Beneficiario agregado exitosamente.";
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Delete(int id)
        {
            var clientId = User.GetUserId();
            await mediator.Send(new DeleteBeneficiaryCommand(id, clientId));
            TempData["SuccessMessage"] = "Beneficiario eliminado exitosamente.";
            return RedirectToAction(nameof(Index));
        }
    }
}
