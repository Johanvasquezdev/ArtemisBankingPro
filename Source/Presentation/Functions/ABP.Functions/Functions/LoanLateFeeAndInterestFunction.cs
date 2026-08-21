using ABP.Core.Application.Features.Functions.Commands;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace ABP.Functions.Functions;

public sealed class LoanLateFeeAndInterestFunction(IMediator mediator, ILogger<LoanLateFeeAndInterestFunction> logger)
{
    [Function(nameof(LoanLateFeeAndInterestFunction))]
    public async Task Run([TimerTrigger("0 30 0 * * *")] TimerInfo timer)
    {
        logger.LogInformation("Loan overdue review started at {Time}.", DateTime.UtcNow);
        var result = await mediator.Send(new RunLoanLateFeeAndInterestCommand());
        logger.LogInformation("Loan overdue review finished. Marked: {Marked}; cleared: {Cleared}.", result.MarkedOverdue, result.ClearedOverdue);
        if (timer.ScheduleStatus is not null)
            logger.LogInformation("Next schedule at: {Next}.", timer.ScheduleStatus.Next);
    }
}
