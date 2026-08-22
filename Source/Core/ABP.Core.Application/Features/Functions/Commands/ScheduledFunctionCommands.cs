using System.Text.Json;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using MediatR;

namespace ABP.Core.Application.Features.Functions.Commands;

public sealed record RunCreditCardBillingCycleCommand : IRequest<int>;

public sealed class RunCreditCardBillingCycleCommandHandler(ICreditCardRepository cards)
    : IRequestHandler<RunCreditCardBillingCycleCommand, int>
{
    public async Task<int> Handle(RunCreditCardBillingCycleCommand request, CancellationToken cancellationToken)
        => (await cards.GetAllAsync()).Count(card => card.Status == CardStatus.Active);
}

public sealed record RunLoanLateFeeAndInterestCommand : IRequest<LoanOverdueResult>;
public sealed record LoanOverdueResult(int MarkedOverdue, int ClearedOverdue);

public sealed class RunLoanLateFeeAndInterestCommandHandler(
    ILoanInstallmentRepository installments,
    ILoanRepository loans,
    IUnitOfWork unitOfWork)
    : IRequestHandler<RunLoanLateFeeAndInterestCommand, LoanOverdueResult>
{
    public async Task<LoanOverdueResult> Handle(RunLoanLateFeeAndInterestCommand request, CancellationToken cancellationToken)
    {
        var marked = 0;
        var cleared = 0;
        await using var transaction = await unitOfWork.BeginTransactionAsync(cancellationToken);

        var activeLoanIds = (await loans.GetAllAsync())
            .Where(loan => loan.Status == LoanStatus.Active)
            .Select(loan => loan.Id)
            .ToArray();

        foreach (var installment in await installments.GetOverdueInstallmentsByLoanIdsAsync(activeLoanIds))
        {
            if (!installment.IsOverdue)
            {
                installment.IsOverdue = true;
                await installments.UpdateWithoutSaveAsync(installment);
                marked++;
            }
        }

        var activeInstallments = await installments.GetByLoanIdsAsync(activeLoanIds);

        foreach (var installment in activeInstallments)
        {
            if (installment.IsOverdue && installment.AmountPaid >= installment.InstallmentAmount)
            {
                installment.IsOverdue = false;
                await installments.UpdateWithoutSaveAsync(installment);
                cleared++;
            }
        }

        await unitOfWork.SaveChangesAsync(cancellationToken);
        await transaction.CommitAsync(cancellationToken);
        return new LoanOverdueResult(marked, cleared);
    }
}

public sealed record GenerateDailyIndicatorsCommand : IRequest<DailyIndicatorsResult>;
public sealed record DailyIndicatorsResult(int Transactions, int Payments);

public sealed class GenerateDailyIndicatorsCommandHandler(ITransactionQueryService transactions)
    : IRequestHandler<GenerateDailyIndicatorsCommand, DailyIndicatorsResult>
{
    public async Task<DailyIndicatorsResult> Handle(GenerateDailyIndicatorsCommand request, CancellationToken cancellationToken)
        => new(await transactions.GetTodayTransactionsCountAsync(), await transactions.GetTodayPaymentsCountAsync());
}

public sealed record ProcessEmailMessageCommand(string Message) : IRequest<bool>;

public sealed class ProcessEmailMessageCommandHandler(IEmailServices emailService)
    : IRequestHandler<ProcessEmailMessageCommand, bool>
{
    public async Task<bool> Handle(ProcessEmailMessageCommand request, CancellationToken cancellationToken)
    {
        var email = JsonSerializer.Deserialize<EmailQueueMessage>(request.Message,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });
        if (email is null || string.IsNullOrWhiteSpace(email.To)) return false;
        await emailService.SendAsync(email.To, email.Subject, email.Body);
        return true;
    }
}

public sealed class EmailQueueMessage
{
    public string To { get; set; } = string.Empty;
    public string Subject { get; set; } = string.Empty;
    public string Body { get; set; } = string.Empty;
}

public sealed record RunScheduledPaymentsCommand : IRequest<bool>;

public sealed class RunScheduledPaymentsCommandHandler(IScheduledPaymentService scheduledPaymentService)
    : IRequestHandler<RunScheduledPaymentsCommand, bool>
{
    public async Task<bool> Handle(RunScheduledPaymentsCommand request, CancellationToken cancellationToken)
    {
        await scheduledPaymentService.ExecuteDuePaymentsAsync(DateTime.UtcNow.Day);
        return true;
    }
}

