using ABP.Core.Application.Interfaces.IServices;
using ABP.Infraestructure.identity.Entities;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Areas.Commerce.Controllers
{
    [Area("Commerce")]
    [Authorize(Roles = "Commerce")]
    public class TransactionsController : Controller
    {
        private readonly IPaymentProcessorService _paymentProcessor;
        private readonly UserManager<ApplicationUser> _userManager;

        public TransactionsController(IPaymentProcessorService paymentProcessor, UserManager<ApplicationUser> userManager)
        {
            _paymentProcessor = paymentProcessor;
            _userManager = userManager;
        }

        public async Task<IActionResult> Index(int page = 1)
        {
            var user = await _userManager.GetUserAsync(User);
            if (user == null || user.CommerceId == null)
            {
                return RedirectToAction("Logout", "Login", new { area = "" });
            }

            int pageSize = 10;
            var transactionsResult = await _paymentProcessor.GetCommerceTransactionsAsync(user.CommerceId.Value, page, pageSize);

            return View(transactionsResult);
        }
    }
}
