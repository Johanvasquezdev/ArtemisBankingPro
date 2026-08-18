using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.Features.Admin.Commands;
using ABP.Core.Application.Features.Admin.Queries;
using ABP.Core.Application.ViewModels.Client;
using ABP.Core.Application.ViewModels.CreditCard;
using ABP.Core.Domain.Enums;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArtemisBankingPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("Admin/[controller]")]
    public class CreditCardManagementController(IMediator mediator,
        ILogger<CreditCardManagementController>? logger = null) : Controller
    {
        private readonly IMediator _mediator = mediator;
        private readonly ILogger<CreditCardManagementController> _logger = logger ?? NullLogger<CreditCardManagementController>.Instance;

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int page = 1, CardStatus? status = CardStatus.Active, string? cedula = null)
        {
            var result = await _mediator.Send(new GetAdminCreditCardsQuery(page, 20, status, cedula));
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentCedula = cedula;
            return View(result);
        }

        [HttpGet("SelectClient")]
        public async Task<IActionResult> SelectClient(string? cedula = null)
        {
            var clients = await _mediator.Send(new GetActiveClientsQuery(cedula));
            var vm = new SelectClientViewModel { Clients = clients, CurrentCedula = cedula };
            return View(vm);
        }

        [HttpGet("Assign/{clientId}")]
        public IActionResult Assign(string clientId)
        {
            return View(new AssignCreditCardViewModel { ClientId = clientId });
        }

        [HttpPost("Assign/{clientId?}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignCreditCardViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            try
            {
                await _mediator.Send(new AssignCreditCardCommand(model.ClientId, model.CreditLimit));
                TempData["SuccessMessage"] = "Tarjeta de crédito asignada correctamente.";
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
                _logger.LogError(ex, "Error al asignar tarjeta para el cliente {ClientId}", model.ClientId);
                model.HasError = true;
                model.Error = "No se pudo completar la asignación de la tarjeta. Vuelve a intentarlo.";
                return View(model);
            }
        }

        [HttpGet("Details/{id:int}")]
        public async Task<IActionResult> Details(int id)
        {
            var details = await _mediator.Send(new GetAdminCreditCardDetailsQuery(id));
            if (details == null) return NotFound();

            return View(new CreditCardDetailViewModel { CreditCard = details.Card, Consumptions = details.Consumptions });
        }

        [HttpGet("EditLimit/{id:int}")]
        public async Task<IActionResult> EditLimit(int id)
        {
            var card = (await _mediator.Send(new GetAdminCreditCardDetailsQuery(id)))?.Card;
            if (card == null) return NotFound();

            var vm = new EditCreditCardLimitViewModel
            {
                CardId = id,
                CardNumber = card.CardNumber,
                CurrentCreditLimit = card.CreditLimit,
                NewCreditLimit = card.CreditLimit,
                AmountOwed = card.AmountOwed
            };

            return View(vm);
        }

        [HttpPost("EditLimit/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> EditLimit(int id, EditCreditCardLimitViewModel model)
        {
            if (id != model.CardId) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            try
            {
                await _mediator.Send(new UpdateCreditCardLimitCommand(id, model.NewCreditLimit));
                TempData["SuccessMessage"] = "Límite de crédito actualizado correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                model.HasError = true;
                model.Error = ex.Message;
                return View(model);
            }
        }

        [HttpGet("Cancel/{id:int}")]
        public async Task<IActionResult> Cancel(int id)
        {
            var card = (await _mediator.Send(new GetAdminCreditCardDetailsQuery(id)))?.Card;
            if (card == null) return NotFound();

            var vm = new CancelCreditCardViewModel
            {
                CardId = id,
                LastFourDigits = card.CardNumber.Length >= 4 ? card.CardNumber[^4..] : card.CardNumber,
                AmountOwed = card.AmountOwed
            };

            return View(vm);
        }

        [HttpPost("Cancel/{id:int}")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CancelConfirmed(int id)
        {
            try
            {
                await _mediator.Send(new CancelCreditCardCommand(id));
                TempData["SuccessMessage"] = "Tarjeta cancelada correctamente.";
            }
            catch (InvalidOperationException ex)
            {
                TempData["ErrorMessage"] = ex.Message;
            }

            return RedirectToAction(nameof(Index));
        }
    }
}
