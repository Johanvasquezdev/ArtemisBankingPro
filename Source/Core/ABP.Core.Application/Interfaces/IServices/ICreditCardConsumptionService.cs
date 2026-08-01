using ABP.Core.Application.DTOs.CreditCardConsumption;

namespace ABP.Core.Application.Interfaces.IServices
{
    public interface ICreditCardConsumptionService
    {
        Task<CreditCardConsumptionDto> GetByIdAsync(int id);
        Task<IEnumerable<CreditCardConsumptionDto>> GetByCardIdAsync(int creditCardId);
        Task AddAsync(CreditCardConsumptionDto dto);
    }
}
