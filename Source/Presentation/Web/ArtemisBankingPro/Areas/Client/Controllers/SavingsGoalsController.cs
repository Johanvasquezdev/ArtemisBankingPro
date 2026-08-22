using ABP.Core.Application.DTOs.SavingsGoal;
using ABP.Core.Application.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;

namespace ArtemisBankingPro.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize(Roles = "Client")]
    public class SavingsGoalsController(ISavingsGoalService savingsGoalService, ISavingsAccountService savingsAccountService) : Controller
    {
        private readonly ISavingsGoalService _savingsGoalService = savingsGoalService;
        private readonly ISavingsAccountService _savingsAccountService = savingsAccountService;

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var accounts = await _savingsAccountService.GetByClientIdAsync(userId);
            var account = accounts.FirstOrDefault(a => a.IsMain);
            if (account == null) account = accounts.FirstOrDefault();

            if (account == null) return View(new List<SavingsGoalDto>());

            var goals = await _savingsGoalService.GetBySavingsAccountIdAsync(account.Id);
            ViewBag.MainAccountId = account.Id;
            return View(goals);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateSavingsGoalDto model)
        {
            if (ModelState.IsValid)
            {
                await _savingsGoalService.CreateAsync(model);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Deposit(int goalId, decimal amount)
        {
            if (amount > 0)
            {
                await _savingsGoalService.AddFundsAsync(goalId, amount);
            }
            return RedirectToAction(nameof(Index));
        }
    }
}
