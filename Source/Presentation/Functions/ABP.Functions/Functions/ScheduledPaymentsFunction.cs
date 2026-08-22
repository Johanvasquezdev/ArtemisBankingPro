using ABP.Core.Application.Features.Functions.Commands;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System;
using System.Threading.Tasks;

namespace ABP.Functions.Functions;

public sealed class ScheduledPaymentsFunction(IMediator mediator, ILogger<ScheduledPaymentsFunction> logger)
{
    [Function(nameof(ScheduledPaymentsFunction))]
    public async Task Run([TimerTrigger("0 0 10 * * *")] TimerInfo timer)
    {
        try
        {
            logger.LogInformation("Executing Scheduled Payments...");
            await mediator.Send(new RunScheduledPaymentsCommand());
            logger.LogInformation("Scheduled Payments executed successfully.");
        }
        catch (Exception exception)
        {
            logger.LogError(exception, "Error executing Scheduled Payments.");
            throw;
        }
    }
}