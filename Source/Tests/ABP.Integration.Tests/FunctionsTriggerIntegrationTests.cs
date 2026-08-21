using ABP.Core.Application.Features.Functions.Commands;
using ABP.Functions.Functions;
using FluentAssertions;
using MediatR;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ABP.Integration.Tests;

public sealed class FunctionsTriggerIntegrationTests
{
    [Fact]
    public async Task TimerTriggers_ShouldExecuteTheirRealFunctionEntryPoints()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(item => item.Send(It.IsAny<RunCreditCardBillingCycleCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(3);
        mediator.Setup(item => item.Send(It.IsAny<RunLoanLateFeeAndInterestCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new LoanOverdueResult(2, 1));
        mediator.Setup(item => item.Send(It.IsAny<GenerateDailyIndicatorsCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(new DailyIndicatorsResult(12, 4));

        var timer = new TimerInfo();
        await new CreditCardBillingCycleFunction(mediator.Object, NullLogger<CreditCardBillingCycleFunction>.Instance).Run(timer);
        await new LoanLateFeeAndInterestFunction(mediator.Object, NullLogger<LoanLateFeeAndInterestFunction>.Instance).Run(timer);
        await new DailyIndicatorFunction(mediator.Object, NullLogger<DailyIndicatorFunction>.Instance).Run(timer);

        mediator.Verify(item => item.Send(It.IsAny<RunCreditCardBillingCycleCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        mediator.Verify(item => item.Send(It.IsAny<RunLoanLateFeeAndInterestCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        mediator.Verify(item => item.Send(It.IsAny<GenerateDailyIndicatorsCommand>(), It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task QueueTrigger_ShouldDispatchTheReceivedEmailMessage()
    {
        var mediator = new Mock<IMediator>();
        mediator.Setup(item => item.Send(It.IsAny<ProcessEmailMessageCommand>(), It.IsAny<CancellationToken>()))
            .ReturnsAsync(true);
        var message = "{\"to\":\"recipient@test.local\",\"subject\":\"Artemis\",\"body\":\"Test\"}";

        await new EmailSenderFunction(mediator.Object, NullLogger<EmailSenderFunction>.Instance).Run(message);

        mediator.Verify(item => item.Send(
            It.Is<ProcessEmailMessageCommand>(command => command.Message == message),
            It.IsAny<CancellationToken>()), Times.Once);
    }
}
