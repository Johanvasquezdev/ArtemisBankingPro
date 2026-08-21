using ABP.Core.Application.DTOs.CreditCardConsumption;

namespace ABP.Core.Application.Interfaces.IServices
{
    public interface ICreditCardConsumptionService
    {
        Task<CreditCardConsumptionDto> GetByIdAsync(int id);
        Task<IEnumerable<CreditCardConsumptionDto>> GetByCardIdAsync(int creditCardId);
        Task<IEnumerable<CreditCardConsumptionDto>> GetByCommerceIdAsync(int commerceId);
        Task<(IEnumerable<CreditCardConsumptionDto> Items, int TotalCount)> GetByCommerceIdPagedAsync(int commerceId, int page, int pageSize);
        Task<CreditCardConsumptionDto> AddAsync(CreditCardConsumptionDto dto);
        /// <summary>Tracks a consumption in the current unit of work without flushing it.</summary>
        Task<CreditCardConsumptionDto> AddWithoutSaveAsync(CreditCardConsumptionDto dto);
    }
}
