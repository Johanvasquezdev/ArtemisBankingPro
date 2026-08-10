using ABP.Core.Application.DTOs.Common;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Client.Commands
{
    public record CashAdvanceCommand(CashAdvanceDto Dto) : IRequest<CommandResult>;

    public class CashAdvanceCommandValidator : AbstractValidator<CashAdvanceCommand>
    {
        public CashAdvanceCommandValidator()
        {
            RuleFor(x => x.Dto.ClientId).NotEmpty().WithMessage("El usuario no es válido.");
            RuleFor(x => x.Dto.CreditCardId)
                .GreaterThan(0).WithMessage("La tarjeta de crédito es requerida.");
            RuleFor(x => x.Dto.SavingsAccountId)
                .GreaterThan(0).WithMessage("La cuenta de ahorro es requerida.");
            RuleFor(x => x.Dto.Amount)
                .GreaterThan(0).WithMessage("El monto del avance debe ser mayor a cero.");
        }
    }

    public class CashAdvanceCommandHandler(ITransactionService transactionService)
        : IRequestHandler<CashAdvanceCommand, CommandResult>
    {
        public Task<CommandResult> Handle(CashAdvanceCommand request, CancellationToken cancellationToken)
            => transactionService.CashAdvanceAsync(request.Dto);
    }
}
