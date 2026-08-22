using ABP.Core.Application.DTOs.ScheduledPayment;
using ABP.Core.Application.Interfaces.IServices;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using System.Threading.Tasks;
using System.Linq;
using System.Collections.Generic;

namespace ArtemisBankingPro.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize(Roles = "Client")]
    public class ScheduledPaymentsController(IScheduledPaymentService scheduledPaymentService, ISavingsAccountService savingsAccountService) : Controller
    {
        private readonly IScheduledPaymentService _scheduledPaymentService = scheduledPaymentService;
        private readonly ISavingsAccountService _savingsAccountService = savingsAccountService;

        public async Task<IActionResult> Index()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier);
            var accounts = await _savingsAccountService.GetByClientIdAsync(userId);
            var account = accounts.FirstOrDefault(a => a.Type == ABP.Core.Domain.Enums.AccountType.Primary);
            if (account == null) account = accounts.FirstOrDefault();

            if (account == null) return View(new List<ScheduledPaymentDto>());

            var payments = await _scheduledPaymentService.GetBySavingsAccountIdAsync(account.Id);
            ViewBag.MainAccountId = account.Id;
            return View(payments);
        }

        [HttpPost]
        public async Task<IActionResult> Create(CreateScheduledPaymentDto model)
        {
            if (ModelState.IsValid)
            {
                await _scheduledPaymentService.CreateAsync(model);
            }
            return RedirectToAction(nameof(Index));
        }

        [HttpPost]
        public async Task<IActionResult> Toggle(int id)
        {
            await _scheduledPaymentService.ToggleActiveAsync(id);
            return RedirectToAction(nameof(Index));
        }
    }
}
