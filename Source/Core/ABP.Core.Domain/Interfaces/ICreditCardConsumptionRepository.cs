using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces.IGenerics;

namespace ABP.Core.Domain.Interfaces
{
    public interface ICreditCardConsumptionRepository : IGenericRepository<CreditCardConsumption>
    {
        Task<IEnumerable<CreditCardConsumption>> GetByCardIdAsync(int creditCardId);
        Task<IEnumerable<CreditCardConsumption>> GetByCommerceIdAsync(int commerceId);
    }
}
