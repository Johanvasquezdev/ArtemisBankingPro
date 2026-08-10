using ABP.Core.Application.DTOs.Common;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Client.Commands
{
    public record PayLoanCommand(PayLoanDto Dto) : IRequest<CommandResult>;

    public class PayLoanCommandValidator : AbstractValidator<PayLoanCommand>
    {
        public PayLoanCommandValidator()
        {
            RuleFor(x => x.Dto.ClientId).NotEmpty().WithMessage("El usuario no es válido.");
            RuleFor(x => x.Dto.SourceAccountNumber)
                .NotEmpty().WithMessage("La cuenta de origen es requerida.")
                .Length(9).WithMessage("El número de cuenta de origen no es válido.");
            RuleFor(x => x.Dto.LoanNumber)
                .NotEmpty().WithMessage("El préstamo es requerido.");
            RuleFor(x => x.Dto.Amount)
                .GreaterThan(0).WithMessage("El monto debe ser mayor a cero.");
        }
    }

    public class PayLoanCommandHandler(ITransactionService transactionService)
        : IRequestHandler<PayLoanCommand, CommandResult>
    {
        public Task<CommandResult> Handle(PayLoanCommand request, CancellationToken cancellationToken)
            => transactionService.PayLoanAsync(request.Dto);
    }
}
