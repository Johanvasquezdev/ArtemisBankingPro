using ABP.Core.Application.DTOs.Common;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Client.Commands
{
    public record PayCreditCardCommand(PayCreditCardDto Dto) : IRequest<CommandResult>;

    public class PayCreditCardCommandValidator : AbstractValidator<PayCreditCardCommand>
    {
        public PayCreditCardCommandValidator()
        {
            RuleFor(x => x.Dto.ClientId).NotEmpty().WithMessage("El usuario no es válido.");
            RuleFor(x => x.Dto.SourceAccountNumber)
                .NotEmpty().WithMessage("La cuenta de origen es requerida.")
                .Length(9).WithMessage("El número de cuenta de origen no es válido.");
            RuleFor(x => x.Dto.CreditCardNumber)
                .NotEmpty().WithMessage("La tarjeta de crédito es requerida.")
                .Length(16).WithMessage("El número de tarjeta no es válido.");
            RuleFor(x => x.Dto.Amount)
                .GreaterThan(0).WithMessage("El monto debe ser mayor a cero.");
        }
    }

    public class PayCreditCardCommandHandler(ITransactionService transactionService)
        : IRequestHandler<PayCreditCardCommand, CommandResult>
    {
        public Task<CommandResult> Handle(PayCreditCardCommand request, CancellationToken cancellationToken)
            => transactionService.PayCreditCardAsync(request.Dto);
    }
}
