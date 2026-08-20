using ABP.Core.Application.DTOs.Cashier;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Cashier.Commands
{
    public sealed record DepositCashierCommand(CashierDepositDto Dto) : IRequest<Unit>;
    public sealed record WithdrawCashierCommand(CashierWithdrawalDto Dto) : IRequest<Unit>;
    public sealed record PayCashierCreditCardCommand(CashierPayCreditCardDto Dto) : IRequest<Unit>;
    public sealed record PayCashierLoanCommand(CashierPayLoanDto Dto) : IRequest<Unit>;
    public sealed record TransferCashierCommand(CashierTransferDto Dto) : IRequest<Unit>;

    public sealed class DepositCashierCommandValidator : AbstractValidator<DepositCashierCommand>
    {
        public DepositCashierCommandValidator()
        {
            RuleFor(x => x.Dto.AccountNumber).NotEmpty().WithMessage("Este campo es obligatorio.");
            RuleFor(x => x.Dto.Amount).GreaterThan(0).WithMessage("El monto debe ser mayor a cero.");
            RuleFor(x => x.Dto.PerformedByUserId).NotEmpty().WithMessage("Este campo es obligatorio.");
            RuleFor(x => x.Dto.IdempotencyKey).NotEmpty().WithMessage("Este campo es obligatorio.").MaximumLength(200);
        }
    }

    public sealed class WithdrawCashierCommandValidator : AbstractValidator<WithdrawCashierCommand>
    {
        public WithdrawCashierCommandValidator()
        {
            RuleFor(x => x.Dto.AccountNumber).NotEmpty().WithMessage("Este campo es obligatorio.");
            RuleFor(x => x.Dto.Amount).GreaterThan(0).WithMessage("El monto debe ser mayor a cero.");
            RuleFor(x => x.Dto.PerformedByUserId).NotEmpty().WithMessage("Este campo es obligatorio.");
            RuleFor(x => x.Dto.IdempotencyKey).NotEmpty().WithMessage("Este campo es obligatorio.").MaximumLength(200);
        }
    }

    public sealed class PayCashierCreditCardCommandValidator : AbstractValidator<PayCashierCreditCardCommand>
    {
        public PayCashierCreditCardCommandValidator()
        {
            RuleFor(x => x.Dto.SourceAccountNumber).NotEmpty().WithMessage("Este campo es obligatorio.");
            RuleFor(x => x.Dto.CardNumber).NotEmpty().WithMessage("Este campo es obligatorio.");
            RuleFor(x => x.Dto.Amount).GreaterThan(0).WithMessage("El monto debe ser mayor a cero.");
            RuleFor(x => x.Dto.PerformedByUserId).NotEmpty().WithMessage("Este campo es obligatorio.");
            RuleFor(x => x.Dto.IdempotencyKey).NotEmpty().WithMessage("Este campo es obligatorio.").MaximumLength(200);
        }
    }

    public sealed class PayCashierLoanCommandValidator : AbstractValidator<PayCashierLoanCommand>
    {
        public PayCashierLoanCommandValidator()
        {
            RuleFor(x => x.Dto.SourceAccountNumber).NotEmpty().WithMessage("Este campo es obligatorio.");
            RuleFor(x => x.Dto.LoanNumber).NotEmpty().WithMessage("Este campo es obligatorio.");
            RuleFor(x => x.Dto.Amount).GreaterThan(0).WithMessage("El monto debe ser mayor a cero.");
            RuleFor(x => x.Dto.PerformedByUserId).NotEmpty().WithMessage("Este campo es obligatorio.");
            RuleFor(x => x.Dto.IdempotencyKey).NotEmpty().WithMessage("Este campo es obligatorio.").MaximumLength(200);
        }
    }

    public sealed class TransferCashierCommandValidator : AbstractValidator<TransferCashierCommand>
    {
        public TransferCashierCommandValidator()
        {
            RuleFor(x => x.Dto.SourceAccountNumber).NotEmpty().WithMessage("Este campo es obligatorio.");
            RuleFor(x => x.Dto.DestinationAccountNumber).NotEmpty().WithMessage("Este campo es obligatorio.");
            RuleFor(x => x.Dto.Amount).GreaterThan(0).WithMessage("El monto debe ser mayor a cero.");
            RuleFor(x => x.Dto.PerformedByUserId).NotEmpty().WithMessage("Este campo es obligatorio.");
            RuleFor(x => x.Dto.IdempotencyKey).NotEmpty().WithMessage("Este campo es obligatorio.").MaximumLength(200);
        }
    }

    public sealed class DepositCashierCommandHandler(ICashierTransactionService transactionService)
        : IRequestHandler<DepositCashierCommand, Unit>
    {
        public async Task<Unit> Handle(DepositCashierCommand request, CancellationToken cancellationToken)
        {
            await transactionService.DepositAsync(request.Dto);
            return Unit.Value;
        }
    }

    public sealed class WithdrawCashierCommandHandler(ICashierTransactionService transactionService)
        : IRequestHandler<WithdrawCashierCommand, Unit>
    {
        public async Task<Unit> Handle(WithdrawCashierCommand request, CancellationToken cancellationToken)
        {
            await transactionService.WithdrawAsync(request.Dto);
            return Unit.Value;
        }
    }

    public sealed class PayCashierCreditCardCommandHandler(ICashierTransactionService transactionService)
        : IRequestHandler<PayCashierCreditCardCommand, Unit>
    {
        public async Task<Unit> Handle(PayCashierCreditCardCommand request, CancellationToken cancellationToken)
        {
            await transactionService.CashierPayCreditCardAsync(request.Dto);
            return Unit.Value;
        }
    }

    public sealed class PayCashierLoanCommandHandler(ICashierTransactionService transactionService)
        : IRequestHandler<PayCashierLoanCommand, Unit>
    {
        public async Task<Unit> Handle(PayCashierLoanCommand request, CancellationToken cancellationToken)
        {
            await transactionService.CashierPayLoanAsync(request.Dto);
            return Unit.Value;
        }
    }

    public sealed class TransferCashierCommandHandler(ICashierTransactionService transactionService)
        : IRequestHandler<TransferCashierCommand, Unit>
    {
        public async Task<Unit> Handle(TransferCashierCommand request, CancellationToken cancellationToken)
        {
            await transactionService.CashierTransferAsync(request.Dto);
            return Unit.Value;
        }
    }
}

