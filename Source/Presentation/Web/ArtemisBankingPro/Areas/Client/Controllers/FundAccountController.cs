using ABP.Core.Application.Interfaces.Services;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ArtemisBankingPro.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Roles = "Client")]
public class FundAccountController : Controller
{
    private readonly IPaymentGatewayService _paymentService;
    private readonly ISavingsAccountService _savingsService;

    public FundAccountController(IPaymentGatewayService paymentService, ISavingsAccountService savingsService)
    {
        _paymentService = paymentService;
        _savingsService = savingsService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var accounts = await _savingsService.GetByClientIdAsync(userId!);
        ViewBag.Accounts = accounts;
        return View();
    }

    [HttpPost]
    public async Task<IActionResult> Checkout(decimal amount, string targetAccountId)
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
        var successUrl = Url.Action("Success", "FundAccount", new { area = "Client" }, Request.Scheme);
        var cancelUrl = Url.Action("Index", "FundAccount", new { area = "Client" }, Request.Scheme);

        var checkoutUrl = await _paymentService.CreatePaymentSessionAsync(
            amount, "DOP", successUrl!, cancelUrl!, userId!, targetAccountId);

        return Redirect(checkoutUrl);
    }

    public IActionResult Success(string session_id)
    {
        ViewBag.SessionId = session_id;
        return View();
    }
}