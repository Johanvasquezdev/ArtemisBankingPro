using ArtemisBankingPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Controllers;

public sealed class HomeController : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Index() => RedirectToAction("Index", "Login");

    [AllowAnonymous]
    [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
    public IActionResult Error()
    {
        var requestId = HttpContext.TraceIdentifier;

        if (Request.Headers.Accept.ToString().Contains(
                "application/problem+json", StringComparison.OrdinalIgnoreCase))
        {
            var problem = new ProblemDetails
            {
                Status = StatusCodes.Status500InternalServerError,
                Title = "Error interno",
                Detail = "Ocurrió un error inesperado. Intente nuevamente más tarde.",
                Instance = HttpContext.Request.Path
            };
            problem.Extensions["traceId"] = requestId;

            return new JsonResult(problem)
            {
                StatusCode = problem.Status,
                ContentType = "application/problem+json"
            };
        }

        return View("~/Views/Shared/Error.cshtml", new ErrorViewModel
        {
            RequestId = requestId
        });
    }
}
