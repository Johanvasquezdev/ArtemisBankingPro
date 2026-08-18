using System.Net;
using System.Text.Json;
using ValidationException = FluentValidation.ValidationException;

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
                response.ContentType = "application/json";

                object responseModel;

                switch (error)
                {
                    case ValidationException validationException:
                        // Lanzada por ValidationBehavior<TRequest,TResponse> cuando un
                        // Command/Query no pasa las reglas de FluentValidation.
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        responseModel = new
                        {
                            message = "Uno o mas campos no son validos.",
                            success = false,
                            errors = validationException.Errors.Select(e => new { field = e.PropertyName, error = e.ErrorMessage })
                        };
                        break;
                    case ApplicationException:
                        // custom application error
                        response.StatusCode = (int)HttpStatusCode.BadRequest;
                        responseModel = new { message = error.Message, success = false };
                        break;
                    case KeyNotFoundException:
                        // not found error
                        response.StatusCode = (int)HttpStatusCode.NotFound;
                        responseModel = new { message = error.Message, success = false };
                        break;
                    default:
                        // unhandled error
                        _logger.LogError(error, "An unhandled exception occurred.");
                        response.StatusCode = (int)HttpStatusCode.InternalServerError;
                        responseModel = new { message = error.Message, success = false };
                        break;
                }

                var result = JsonSerializer.Serialize(responseModel);
                await response.WriteAsync(result);
            }
        }
    }
}
