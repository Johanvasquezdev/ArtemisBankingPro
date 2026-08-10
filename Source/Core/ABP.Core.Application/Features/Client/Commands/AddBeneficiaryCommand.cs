using ABP.Core.Application.DTOs.Beneficiary;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Client.Commands
{
    public record AddBeneficiaryCommand(string OwnerId, string AccountNumber) : IRequest<BeneficiaryDto>;

    public class AddBeneficiaryCommandValidator : AbstractValidator<AddBeneficiaryCommand>
    {
        public AddBeneficiaryCommandValidator()
        {
            RuleFor(x => x.OwnerId).NotEmpty().WithMessage("El usuario no es válido.");
            RuleFor(x => x.AccountNumber)
                .NotEmpty().WithMessage("El número de cuenta es requerido.")
                .Length(9).WithMessage("El número de cuenta no es válido.");
        }
    }

    public class AddBeneficiaryCommandHandler(IBeneficiaryService beneficiaryService)
        : IRequestHandler<AddBeneficiaryCommand, BeneficiaryDto>
    {
        public Task<BeneficiaryDto> Handle(AddBeneficiaryCommand request, CancellationToken cancellationToken)
            => beneficiaryService.AddAsync(request.OwnerId, request.AccountNumber);
    }
}
