using ABP.Core.Application.DTOs.Common;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Client.Commands
{
    public record PayBeneficiaryCommand(PayBeneficiaryDto Dto) : IRequest<CommandResult>;

    public class PayBeneficiaryCommandValidator : AbstractValidator<PayBeneficiaryCommand>
    {
        public PayBeneficiaryCommandValidator()
        {
            RuleFor(x => x.Dto.ClientId).NotEmpty().WithMessage("El usuario no es válido.");
            RuleFor(x => x.Dto.BeneficiaryId)
                .GreaterThan(0).WithMessage("Debe seleccionar un beneficiario.");
            RuleFor(x => x.Dto.SourceAccountNumber)
                .NotEmpty().WithMessage("La cuenta de origen es requerida.")
                .Length(9).WithMessage("El número de cuenta de origen no es válido.");
            RuleFor(x => x.Dto.Amount)
                .GreaterThan(0).WithMessage("El monto debe ser mayor a cero.");
        }
    }

    public class PayBeneficiaryCommandHandler(IClientTransactionService transactionService)
        : IRequestHandler<PayBeneficiaryCommand, CommandResult>
    {
        public Task<CommandResult> Handle(PayBeneficiaryCommand request, CancellationToken cancellationToken)
            => transactionService.PayBeneficiaryAsync(request.Dto);
    }
}
