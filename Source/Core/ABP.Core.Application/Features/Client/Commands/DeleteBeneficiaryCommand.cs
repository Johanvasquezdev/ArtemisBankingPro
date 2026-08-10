using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Client.Commands
{
    public record DeleteBeneficiaryCommand(int BeneficiaryId, string OwnerId) : IRequest<Unit>;

    public class DeleteBeneficiaryCommandValidator : AbstractValidator<DeleteBeneficiaryCommand>
    {
        public DeleteBeneficiaryCommandValidator()
        {
            RuleFor(x => x.BeneficiaryId)
                .GreaterThan(0).WithMessage("El beneficiario no es válido.");
            RuleFor(x => x.OwnerId).NotEmpty().WithMessage("El usuario no es válido.");
        }
    }

    public class DeleteBeneficiaryCommandHandler(IBeneficiaryService beneficiaryService)
        : IRequestHandler<DeleteBeneficiaryCommand, Unit>
    {
        public async Task<Unit> Handle(DeleteBeneficiaryCommand request, CancellationToken cancellationToken)
        {
            await beneficiaryService.DeleteAsync(request.BeneficiaryId, request.OwnerId);
            return Unit.Value;
        }
    }
}
