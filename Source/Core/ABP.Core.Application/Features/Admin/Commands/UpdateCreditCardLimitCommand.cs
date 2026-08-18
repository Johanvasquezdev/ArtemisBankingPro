using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    public sealed record UpdateCreditCardLimitCommand(int CardId, decimal NewLimit) : IRequest;

    public sealed class UpdateCreditCardLimitCommandValidator : AbstractValidator<UpdateCreditCardLimitCommand>
    {
        public UpdateCreditCardLimitCommandValidator()
        {
            RuleFor(x => x.NewLimit).GreaterThan(0).WithMessage("El nuevo límite debe ser mayor que cero.");
        }
    }

    // InvalidOperationException (limite menor a deuda actual) y "no existe" se propagan
    // tal cual desde el Service; el controller los captura igual que antes.
    public sealed class UpdateCreditCardLimitCommandHandler(ICreditCardService creditCardService)
        : IRequestHandler<UpdateCreditCardLimitCommand>
    {
        private readonly ICreditCardService _creditCardService = creditCardService;

        public async Task Handle(UpdateCreditCardLimitCommand request, CancellationToken cancellationToken)
        {
            await _creditCardService.UpdateLimitAsync(request.CardId, request.NewLimit);
        }
    }
}
