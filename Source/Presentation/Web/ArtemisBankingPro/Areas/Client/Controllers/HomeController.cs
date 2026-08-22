using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.Features.Client.Queries;
using ArtemisBankingPro.Extensions;
using ArtemisBankingPro.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;
using System;

namespace ArtemisBankingPro.Areas.Client.Controllers
{
    [Area("Client")]
    [Authorize(Roles = "Client")]
    public class HomeController(IMediator mediator, ILogger<HomeController> logger, IPersonalFinanceService personalFinanceService) : Controller
    {
        private readonly ILogger<HomeController> _logger = logger;

        public async Task<IActionResult> Index()
        {
            var clientId = User.GetUserId();
            if (string.IsNullOrEmpty(clientId))
                return RedirectToAction("AccessDenied", "Login", new { area = "" });

            var now = DateTime.Now;
            var chartData = await personalFinanceService.GetExpensesByCategoryAsync(clientId, now.Month, now.Year);
            ViewBag.ChartData = chartData;

            var model = await mediator.Send(new GetClientHomeQuery(clientId));
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AccountDetail(string accountNumber, DateTime? dateFrom, DateTime? dateTo)
        {
            var clientId = User.GetUserId();
            if (string.IsNullOrEmpty(clientId))
                return RedirectToAction("AccessDenied", "Login", new { area = "" });

            var model = await mediator.Send(new GetAccountDetailQuery(clientId, accountNumber, dateFrom, dateTo));
            return View(model);
        }


        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
