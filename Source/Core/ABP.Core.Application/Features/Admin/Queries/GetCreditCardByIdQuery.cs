using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.CreditCardConsumption;
using ABP.Core.Application.Interfaces.IServices;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Queries
{
    public sealed record GetCreditCardByIdQuery(int Id) : IRequest<GetCreditCardByIdResult?>;

    public sealed record GetCreditCardByIdResult(CreditCardDto Card, IEnumerable<CreditCardConsumptionDto> Consumptions);

    public sealed class GetCreditCardByIdQueryHandler(ICreditCardService creditCardService, ICreditCardConsumptionService consumptionService)
        : IRequestHandler<GetCreditCardByIdQuery, GetCreditCardByIdResult?>
    {
        private readonly ICreditCardService _creditCardService = creditCardService;
        private readonly ICreditCardConsumptionService _consumptionService = consumptionService;

        public async Task<GetCreditCardByIdResult?> Handle(GetCreditCardByIdQuery request, CancellationToken cancellationToken)
        {
            var card = await _creditCardService.GetByIdAsync(request.Id);
            if (card == null) return null;

            var consumptions = await _consumptionService.GetByCardIdAsync(request.Id);
            return new GetCreditCardByIdResult(card, consumptions);
        }
    }
}
