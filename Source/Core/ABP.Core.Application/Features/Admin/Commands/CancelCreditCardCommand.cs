using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    public sealed record CancelCreditCardCommand(int CardId) : IRequest;

    public sealed class CancelCreditCardCommandValidator : AbstractValidator<CancelCreditCardCommand>
    {
        public CancelCreditCardCommandValidator() => RuleFor(x => x.CardId).GreaterThan(0);
    }

    public sealed class CancelCreditCardCommandHandler(ICreditCardService creditCardService) : IRequestHandler<CancelCreditCardCommand>
    {
        private readonly ICreditCardService _creditCardService = creditCardService;

        public async Task Handle(CancelCreditCardCommand request, CancellationToken cancellationToken)
        {
            await _creditCardService.CancelAsync(request.CardId);
        }
    }
}
