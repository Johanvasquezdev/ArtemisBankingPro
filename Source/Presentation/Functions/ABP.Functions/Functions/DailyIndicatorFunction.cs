using ABP.Core.Application.Features.Functions.Commands;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABP.Functions.Functions;

public sealed class DailyIndicatorFunction(IMediator mediator, ILogger<DailyIndicatorFunction> logger)
{
    [Function(nameof(DailyIndicatorFunction))]
    public async Task Run([TimerTrigger("0 0 0 * * *")] TimerInfo timer)
    {
        try
        {
            var result = await mediator.Send(new GenerateDailyIndicatorsCommand());
            logger.LogInformation("Daily indicators calculated. Transactions: {Transactions}; payments: {Payments}.", result.Transactions, result.Payments);
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error calculating daily indicators.");
            throw;
        }

        if (timer.ScheduleStatus is not null)
            logger.LogInformation("Next schedule at: {Next}.", timer.ScheduleStatus.Next);
    }
}
