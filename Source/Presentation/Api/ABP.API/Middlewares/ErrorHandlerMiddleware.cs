using System.Net;
using System.Text.Json;
using ABP.Core.Domain.Exceptions;
using FluentValidation;
using Microsoft.AspNetCore.Mvc;

namespace ABP.API.Middlewares
{
    public class ErrorHandlerMiddleware
    {
        private readonly RequestDelegate _next;
        private readonly ILogger<ErrorHandlerMiddleware> _logger;

        public ErrorHandlerMiddleware(RequestDelegate next, ILogger<ErrorHandlerMiddleware> logger)
        {
            _next = next;
            _logger = logger;
        }

        public async Task Invoke(HttpContext context)
        {
            try
            {
                await _next(context);
            }
            catch (Exception error)
            {
                var response = context.Response;
                var (status, title, detail) = error switch
                {
                    DomainException or ValidationException or ApplicationException or InvalidOperationException =>
                        ((int)HttpStatusCode.BadRequest, "Solicitud inválida", error.Message),
                    KeyNotFoundException =>
                        ((int)HttpStatusCode.NotFound, "Recurso no encontrado", "El recurso solicitado no existe."),
                    _ => ((int)HttpStatusCode.InternalServerError, "Error interno", "Ocurrió un error inesperado. Intente nuevamente más tarde.")
                };

                if (status >= 500)
                    _logger.LogError(error, "Unhandled API exception. TraceId: {TraceId}", context.TraceIdentifier);
                else
                    _logger.LogWarning(error, "Handled API exception. TraceId: {TraceId}", context.TraceIdentifier);

                response.StatusCode = status;
                response.ContentType = "application/problem+json";
                var problem = new ProblemDetails
                {
                    Status = status,
                    Title = title,
                    Detail = detail,
                    Instance = context.Request.Path
                };
                problem.Extensions["traceId"] = context.TraceIdentifier;
                await JsonSerializer.SerializeAsync(response.Body, problem);
            }
        }
    }
}
