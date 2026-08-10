using ABP.Core.Application.DTOs.Beneficiary;

namespace ABP.Core.Application.Interfaces.IServices
{
    public interface IBeneficiaryService
    {
        Task<BeneficiaryDto> GetByIdAsync(int id);
        Task<IEnumerable<BeneficiaryDto>> GetByOwnerIdAsync(string ownerId);
        Task<BeneficiaryDto> AddAsync(string ownerId, string accountNumber);
        Task DeleteAsync(int id, string ownerId);
        Task<bool> BeneficiaryExistsForOwnerAsync(string ownerId, string accountNumber);
    }
}
