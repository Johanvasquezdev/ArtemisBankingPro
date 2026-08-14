using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.ViewModels.Dashboard;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("Admin/[controller]")]
    public class DashboardController(IDashboardService dashboardService) : Controller
    {
        private readonly IDashboardService _dashboardService = dashboardService;

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var dto = await _dashboardService.GetAdminDashboardAsync();

            var vm = new AdminDashboardViewModel
            {
                TotalTransactions = dto.TotalTransactions,
                TodayTransactions = dto.TodayTransactions,
                TotalProducts = dto.TotalProducts,
                ActiveLoans = dto.ActiveLoans,
                ActiveCreditCards = dto.ActiveCreditCards,
                TotalSavingsAccounts = dto.TotalSavingsAccounts,
                TotalDailyPayments = dto.TodayPayments,
                TotalAssignedProducts = dto.TotalProducts,
                TotalActiveClients = dto.ActiveClients,
                TotalInactiveClients = dto.InactiveClients,
                AverageDebt = dto.AverageDebt
            };

            return View(vm);
        }
    }
}
