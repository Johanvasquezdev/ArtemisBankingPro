using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.CreditCardConsumption;

namespace ABP.Core.Application.ViewModels.Account
{
    public class CardDetailViewModel
    {
        public IEnumerable<CreditCardConsumptionDto> Consumptions { get; set; } = new List<CreditCardConsumptionDto>();
        public CreditCardDto CreditCard { get; set; } = new CreditCardDto();
    }
}
