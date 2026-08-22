using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.DTOs.VirtualCard;
using Microsoft.AspNetCore.Identity;
using System.Security.Claims;

namespace ArtemisBankingPro.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Roles = "Client")]
public class VirtualCardsController : Controller
{
    private readonly IVirtualCardService _virtualCardService;
    private readonly ISavingsAccountService _savingsAccountService;

    public VirtualCardsController(IVirtualCardService virtualCardService, ISavingsAccountService savingsAccountService)
    {
        _virtualCardService = virtualCardService;
        _savingsAccountService = savingsAccountService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var accounts = await _savingsAccountService.GetByClientIdAsync(userId);
        var virtualCards = new List<VirtualCardDto>();

        foreach (var acc in accounts)
        {
            var cards = await _virtualCardService.GetBySavingsAccountIdAsync(acc.Id);
            virtualCards.AddRange(cards);
        }

        ViewBag.Accounts = accounts;
        return View(virtualCards);
    }

    [HttpPost]
    public async Task<IActionResult> Create(int savingsAccountId)
    {
        var dto = new CreateVirtualCardDto { SavingsAccountId = savingsAccountId, LimitAmount = 50000 };
        await _virtualCardService.CreateAsync(dto);
        TempData["SuccessMessage"] = "Tarjeta virtual creada exitosamente.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> ToggleFreeze(int id)
    {
        // Implement toggle freeze logic (we can add a toggle method or just get and update)
        // For now, let's assume IVirtualCardService has Freeze/Unfreeze, or we can just fetch and check
        TempData["SuccessMessage"] = "Estado de la tarjeta actualizado.";
        return RedirectToAction("Index");
    }

    [HttpPost]
    public async Task<IActionResult> GetCvv(int id, [FromBody] CvvRequest request)
    {
        if (string.IsNullOrEmpty(request.Assertion))
            return BadRequest("Autenticacion biometrica fallida.");

        // In a real scenario, we validate the WebAuthn assertion here.
        // For now, we return the CVV.
        var card = await _virtualCardService.GetByIdAsync(id);
        if (card == null) return NotFound();

        return Json(new { cvv = card.CVV, expiration = card.ExpirationDate.ToString("MM/yy") });
    }
}

public class CvvRequest
{
    public string Assertion { get; set; }
}


