using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.CreditCardConsumption;

namespace ABP.Core.Application.ViewModels.CreditCard
{
    public class CreditCardDetailViewModel
    {
        public CreditCardDto CreditCard { get; set; } = null!;
        public IEnumerable<CreditCardConsumptionDto> Consumptions { get; set; } = [];
    }
}
