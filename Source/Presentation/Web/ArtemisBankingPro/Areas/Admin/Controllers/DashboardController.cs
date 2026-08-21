using ABP.Core.Application.Features.Admin.Queries;
using ABP.Core.Application.ViewModels.Dashboard;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Areas.Admin.Controllers
{
    [Area("Admin")]
    [Authorize(Roles = "Admin")]
    [Route("Admin/[controller]")]
    public class DashboardController(IMediator mediator) : Controller
    {
        private readonly IMediator _mediator = mediator;

        [HttpGet("")]
        [HttpGet("Index")]
        public async Task<IActionResult> Index()
        {
            var dto = await _mediator.Send(new GetAdminDashboardQuery());

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
