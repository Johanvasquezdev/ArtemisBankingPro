using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using ArtemisBankingPro.Extensions;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using ABP.Infraestructure.identity.Entities;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Areas.Commerce.Controllers
{
    [Area("Commerce")]
    [Authorize(Roles = "Commerce")]
    public class DashboardController : Controller
    {
        private readonly IPaymentProcessorService _paymentProcessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public DashboardController(IPaymentProcessorService paymentProcessor, UserManager<ApplicationUser> userManager)
        {
            _paymentProcessor = paymentProcessor;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index()
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.CommerceId == null)
            {
                return RedirectToAction("Logout", "Login", new { area = "" });
            }

            var transactionsResult = await _paymentProcessor.GetCommerceTransactionsAsync(user.CommerceId.Value, 1, 10);
            
            var totalSalesAmount = transactionsResult.Items.Where(t => t.Status == TransactionStatus.Approved).Sum(t => t.Amount);

            ViewBag.TotalSales = totalSalesAmount;
            ViewBag.TransactionsCount = transactionsResult.TotalCount;
            ViewBag.RecentTransactions = transactionsResult.Items.Take(5);
            ViewBag.CommerceId = user.CommerceId;

            return View();
        }
    }
}
