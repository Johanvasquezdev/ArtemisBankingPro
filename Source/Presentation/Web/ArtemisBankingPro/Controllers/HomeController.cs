using ABP.Core.Application.Features.Client.Queries;
using ABP.Core.Application.ViewModels.Client;
using ArtemisBankingProApp.Extensions;
using ArtemisBankingProApp.Models;
using MediatR;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Diagnostics;

namespace ArtemisBankingProApp.Controllers
{
    [Authorize(Roles = "Client")]
    public class HomeController(IMediator mediator, ILogger<HomeController> logger) : Controller
    {
        private readonly ILogger<HomeController> _logger = logger;

        public async Task<IActionResult> Index()
        {
            var clientId = User.GetUserId();
            if (string.IsNullOrEmpty(clientId))
                return RedirectToAction(nameof(AccessDenied));

            var model = await mediator.Send(new GetClientHomeQuery(clientId));
            return View(model);
        }

        [HttpGet]
        public async Task<IActionResult> AccountDetail(string accountNumber, DateTime? dateFrom, DateTime? dateTo)
        {
            var clientId = User.GetUserId();
            if (string.IsNullOrEmpty(clientId))
                return RedirectToAction(nameof(AccessDenied));

            var model = await mediator.Send(new GetAccountDetailQuery(clientId, accountNumber, dateFrom, dateTo));
            return View(model);
        }

        public IActionResult AccessDenied()
        {
            return View();
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
