using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces.IGenerics;

namespace ABP.Core.Domain.Interfaces
{
    public interface IBeneficiaryRepository : IGenericRepository<Beneficiary>
    {
        // get all beneficiaries for a specific user
        Task<IEnumerable<Beneficiary>> GetByOwnerAccountIdAsync(string userId);
        Task<bool> BeneficiaryExistForOwnerAsync(string ownerId, string accountNumber);
    }
}
