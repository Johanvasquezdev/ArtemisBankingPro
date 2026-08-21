using ABP.Core.Application.Features.Functions.Commands;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABP.Functions.Functions;

public sealed class CreditCardBillingCycleFunction(IMediator mediator, ILogger<CreditCardBillingCycleFunction> logger)
{
    [Function(nameof(CreditCardBillingCycleFunction))]
    public async Task Run([TimerTrigger("0 0 1 1 * *")] TimerInfo timer)
    {
        logger.LogInformation("Credit card billing cycle started at {Time}.", DateTime.UtcNow);
        var processed = await mediator.Send(new RunCreditCardBillingCycleCommand());
        logger.LogInformation("Credit card billing cycle finished. Cards processed: {Count}.", processed);
        if (timer.ScheduleStatus is not null)
            logger.LogInformation("Next schedule at: {Next}.", timer.ScheduleStatus.Next);
    }
}
