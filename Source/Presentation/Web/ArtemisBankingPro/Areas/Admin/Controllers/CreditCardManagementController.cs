using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.ViewModels.Client;
using ABP.Core.Application.ViewModels.CreditCard;
using ABP.Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;

namespace ArtemisBankingPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("Admin/[controller]")]
    public class CreditCardManagementController(ICreditCardService creditCardService,
        ICreditCardConsumptionService consumptionService, IUserReadOnlyService userReadOnlyService,
        ILogger<CreditCardManagementController>? logger = null) : Controller
    {
        private readonly ICreditCardService _creditCardService = creditCardService;
        private readonly ICreditCardConsumptionService _consumptionService = consumptionService;
        private readonly IUserReadOnlyService _userReadOnlyService = userReadOnlyService;
        private readonly ILogger<CreditCardManagementController> _logger = logger ?? NullLogger<CreditCardManagementController>.Instance;

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index(int page = 1, CardStatus? status = CardStatus.Active, string? cedula = null)
        {
            var result = await _creditCardService.GetAllPagedAsync(page, 20, status, cedula);
            ViewBag.CurrentStatus = status;
            ViewBag.CurrentCedula = cedula;
            return View(result);
        }

        [HttpGet("SelectClient")]
        public async Task<IActionResult> SelectClient(string? cedula = null)
        {
            var clients = await _userReadOnlyService.GetActiveClientsAsync(cedula);
            var vm = new SelectClientViewModel { Clients = clients, CurrentCedula = cedula };
            return View(vm);
        }

        [HttpGet("Assign/{clientId}")]
        public IActionResult Assign(string clientId)
        {
            return View(new AssignCreditCardViewModel { ClientId = clientId });
        }

        [HttpPost("Assign")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Assign(AssignCreditCardViewModel model)
        {
            if (!ModelState.IsValid) return View(model);

            var dto = new AssignCreditCardDto { ClientId = model.ClientId, CreditLimit = model.CreditLimit };

            try
            {
                await _creditCardService.AssignAsync(dto);
                TempData["SuccessMessage"] = "Tarjeta de crédito asignada correctamente.";
                return RedirectToAction(nameof(Index));
            }
            catch (InvalidOperationException ex)
            {
                model.HasError = true;
                model.Error = ex.Message;
                return View(model);
            }
            catch (DbUpdateException ex)
            {
                _logger.LogError(ex, "Error de base de datos al asignar tarjeta para el cliente {ClientId}", model.ClientId);
                model.HasError = true;
                model.Error = "No se pudo guardar la tarjeta. Verifica la conexión y vuelve a intentarlo.";
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
            var card = await _creditCardService.GetByIdAsync(id);
            if (card == null) return NotFound();

            var consumptions = await _consumptionService.GetByCardIdAsync(id);

            return View(new CreditCardDetailViewModel { CreditCard = card, Consumptions = consumptions });
        }

        [HttpGet("EditLimit/{id:int}")]
        public async Task<IActionResult> EditLimit(int id)
        {
            var card = await _creditCardService.GetByIdAsync(id);
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
                await _creditCardService.UpdateLimitAsync(id, model.NewCreditLimit);
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
            var card = await _creditCardService.GetByIdAsync(id);
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
                await _creditCardService.CancelAsync(id);
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
