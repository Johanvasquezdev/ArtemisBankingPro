using ABP.Core.Application.DTOs.Cashier;
using ABP.Core.Application.Interfaces.IServices;
using MediatR;

namespace ABP.Core.Application.Features.Cashier.Commands
{
    public sealed record DepositCashierCommand(CashierDepositDto Dto) : IRequest<Unit>;
    public sealed record WithdrawCashierCommand(CashierWithdrawalDto Dto) : IRequest<Unit>;
    public sealed record PayCashierCreditCardCommand(CashierPayCreditCardDto Dto) : IRequest<Unit>;
    public sealed record PayCashierLoanCommand(CashierPayLoanDto Dto) : IRequest<Unit>;
    public sealed record TransferCashierCommand(CashierTransferDto Dto) : IRequest<Unit>;

    public sealed class DepositCashierCommandHandler(ITransactionService transactionService)
        : IRequestHandler<DepositCashierCommand, Unit>
    {
        public async Task<Unit> Handle(DepositCashierCommand request, CancellationToken cancellationToken)
        {
            await transactionService.DepositAsync(request.Dto);
            return Unit.Value;
        }
    }

    public sealed class WithdrawCashierCommandHandler(ITransactionService transactionService)
        : IRequestHandler<WithdrawCashierCommand, Unit>
    {
        public async Task<Unit> Handle(WithdrawCashierCommand request, CancellationToken cancellationToken)
        {
            await transactionService.WithdrawAsync(request.Dto);
            return Unit.Value;
        }
    }

    public sealed class PayCashierCreditCardCommandHandler(ITransactionService transactionService)
        : IRequestHandler<PayCashierCreditCardCommand, Unit>
    {
        public async Task<Unit> Handle(PayCashierCreditCardCommand request, CancellationToken cancellationToken)
        {
            await transactionService.CashierPayCreditCardAsync(request.Dto);
            return Unit.Value;
        }
    }

    public sealed class PayCashierLoanCommandHandler(ITransactionService transactionService)
        : IRequestHandler<PayCashierLoanCommand, Unit>
    {
        public async Task<Unit> Handle(PayCashierLoanCommand request, CancellationToken cancellationToken)
        {
            await transactionService.CashierPayLoanAsync(request.Dto);
            return Unit.Value;
        }
    }

    public sealed class TransferCashierCommandHandler(ITransactionService transactionService)
        : IRequestHandler<TransferCashierCommand, Unit>
    {
        public async Task<Unit> Handle(TransferCashierCommand request, CancellationToken cancellationToken)
        {
            await transactionService.CashierTransferAsync(request.Dto);
            return Unit.Value;
        }
    }
}
