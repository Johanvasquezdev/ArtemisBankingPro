using Microsoft.AspNetCore.Mvc;
using System.Net.Mime;

namespace ABP.API.Controllers
{
    [Route("api/v{version:apiVersion}/[controller]")]
    [ApiController]
    public abstract class BaseApiController : ControllerBase
    {
        protected ObjectResult ApiProblem(int statusCode, string title, string detail, object? extensions = null)
        {
            var httpContext = ControllerContext?.HttpContext;
            var problem = new ProblemDetails
            {
                Status = statusCode,
                Title = title,
                Detail = detail,
                Instance = httpContext?.Request.Path
            };
            problem.Extensions["traceId"] = httpContext?.TraceIdentifier ?? string.Empty;
            if (extensions is not null)
            {
                foreach (var property in extensions.GetType().GetProperties())
                    problem.Extensions[property.Name] = property.GetValue(extensions);
            }

            return new ObjectResult(problem)
            {
                StatusCode = statusCode,
                ContentTypes = { "application/problem+json" }
            };
        }

        protected ObjectResult ValidationProblem(string detail = "La solicitud contiene datos inválidos.")
        {
            var httpContext = ControllerContext?.HttpContext;
            var problem = new ValidationProblemDetails(ModelState)
            {
                Status = StatusCodes.Status400BadRequest,
                Title = "Validación fallida",
                Detail = detail,
                Instance = httpContext?.Request.Path
            };
            problem.Extensions["traceId"] = httpContext?.TraceIdentifier ?? string.Empty;
            return new ObjectResult(problem)
            {
                StatusCode = StatusCodes.Status400BadRequest,
                ContentTypes = { "application/problem+json" }
            };
        }
    }
}
