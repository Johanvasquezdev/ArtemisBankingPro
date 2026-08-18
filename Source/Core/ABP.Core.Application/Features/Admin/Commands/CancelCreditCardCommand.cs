using ABP.Core.Application.Interfaces.IServices;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    public sealed record CancelCreditCardCommand(int CardId) : IRequest;

    public sealed class CancelCreditCardCommandHandler(ICreditCardService creditCardService) : IRequestHandler<CancelCreditCardCommand>
    {
        private readonly ICreditCardService _creditCardService = creditCardService;

        public async Task Handle(CancelCreditCardCommand request, CancellationToken cancellationToken)
        {
            await _creditCardService.CancelAsync(request.CardId);
        }
    }
}
