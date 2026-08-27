using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ABP.Core.Application.Interfaces.IServices;
using System.Security.Claims;

namespace ArtemisBankingPro.Areas.Client.Controllers;

[Area("Client")]
[Authorize(Roles = "Client")]
public class ProfileController : Controller
{
    private readonly IUserReadOnlyService _userService;
    private readonly ISavingsAccountService _savingsService;
    private readonly ICreditCardService _creditCardService;
    private readonly ILoanService _loanService;

    public ProfileController(
        IUserReadOnlyService userService,
        ISavingsAccountService savingsService,
        ICreditCardService creditCardService,
        ILoanService loanService)
    {
        _userService = userService;
        _savingsService = savingsService;
        _creditCardService = creditCardService;
        _loanService = loanService;
    }

    public async Task<IActionResult> Index()
    {
        var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? string.Empty;
        var user = await _userService.GetByIdAsync(userId);
        
        if (user == null) return NotFound();

        var accounts = await _savingsService.GetByClientIdAsync(userId);
        var creditCards = await _creditCardService.GetByClientIdAsync(userId);
        var loans = await _loanService.GetByClientIdAsync(userId);

        ViewBag.Accounts = accounts;
        ViewBag.CreditCards = creditCards;
        ViewBag.Loans = loans;

        return View(user);
    }
}
