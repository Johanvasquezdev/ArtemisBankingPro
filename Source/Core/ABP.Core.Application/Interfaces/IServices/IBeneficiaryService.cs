using ABP.Core.Application.DTOs.Beneficiary;

namespace ABP.Core.Application.Interfaces.IServices
{
    public interface IBeneficiaryService
    {
        Task<BeneficiaryDto> GetByIdAsync(int id);
        Task<IEnumerable<BeneficiaryDto>> GetByOwnerIdAsync(string ownerId);
        Task<bool> AddAsync(string ownerId, string accountNumber);
        Task DeleteAsync(int id);
        Task<bool> BeneficiaryExistsForOwnerAsync(string ownerId, string accountNumber);
    }
}
