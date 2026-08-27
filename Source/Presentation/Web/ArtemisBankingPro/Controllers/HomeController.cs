using ArtemisBankingPro.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace ArtemisBankingPro.Controllers;

public sealed class HomeController : Controller
{
    [AllowAnonymous]
    [HttpGet]
    public IActionResult Index() => View();

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Client() => Portal("Cliente", "Tu patrimonio, bajo una sola mirada.", "Desde Artemis puedes consultar cuentas, movimientos, tarjetas y préstamos, y ejecutar operaciones con confirmaciones claras.", "Acceder al portal", "Cuentas, tarjetas, transferencias y beneficiarios", "Cada operación se presenta con contexto: origen, destino, monto, estado y referencia.");

    [AllowAnonymous]
    [HttpGet]
    public IActionResult HermesPay() => Portal("Hermes Pay", "Una capa de cobro diseñada para comercios afiliados.", "Hermes Pay conecta pagos con tarjeta, consumos y conciliación dentro del ecosistema Artemis Banking Pro.", "Conocer Hermes Pay", "Pagos autorizados y trazables", "Los comercios asociados pueden registrar y administrar sus operaciones desde una experiencia enfocada en control.");

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Administration() => Portal("Administración", "Control central para decisiones precisas.", "El panel administrativo permite gestionar usuarios, productos financieros, comercios y auditoría operativa desde un único punto.", "Iniciar sesión", "Usuarios, productos y operaciones", "Las acciones administrativas se protegen por roles y muestran estados y validaciones dentro de la aplicación.");

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Cashier() => Portal("Cajero físico", "Operaciones de sucursal, con foco en claridad.", "El portal de cajero concentra depósitos, retiros, pagos y transferencias para una atención rápida y trazable.", "Acceder al portal", "Operaciones de ventanilla", "Cada movimiento se registra con su responsable, estado y referencia para facilitar la consulta posterior.");

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Privacy() => View();

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Terms() => View();

    [AllowAnonymous]
    [HttpGet]
    public IActionResult Contact() => View();

    private IActionResult Portal(string name, string title, string description, string actionLabel, string focus, string detail) =>
        View("Experience", new MarketingPortalViewModel(name, title, description, actionLabel, focus, detail));

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
