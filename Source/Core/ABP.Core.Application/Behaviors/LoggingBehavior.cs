using MediatR;
using Microsoft.Extensions.Logging;

namespace ABP.Core.Application.Behaviors
{
    /// <summary>
    /// Logs every Command/Query execution (financial operations) without registering
    /// sensitive data (CVC, passwords, tokens).
    /// </summary>
    public class LoggingBehavior<TRequest, TResponse>(ILogger<LoggingBehavior<TRequest, TResponse>> logger)
        : IPipelineBehavior<TRequest, TResponse>
        where TRequest : IRequest<TResponse>
    {
        private readonly ILogger<LoggingBehavior<TRequest, TResponse>> _logger = logger;

        public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
        {
            var requestName = typeof(TRequest).Name;
            _logger.LogInformation("Handling {RequestName}", requestName);

            try
            {
                var response = await next();
                _logger.LogInformation("Handled {RequestName} successfully", requestName);
                return response;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Handling {RequestName} failed", requestName);
                throw;
            }
        }
    }
}
