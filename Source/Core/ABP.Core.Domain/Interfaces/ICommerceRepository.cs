using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces.IGenerics;

namespace ABP.Core.Domain.Interfaces
{
    public interface ICommerceRepository : IGenericRepository<Commerce>
    {
        Task<Commerce?> GetByIdWithUserAsync(int commerceId);
        Task<IEnumerable<Commerce>> GetAllPagedAsync(int? page = null, int? pageSize = null, bool? isActive = null);
        Task<bool> ExistsByRncAsync(string rnc, int? excludingId = null);
        Task<bool> ExistsByEmailAsync(string email, int? excludingId = null); 
        Task<bool> CommerceHasActiveUserAsync(int commerceId);
        Task<AssociatedUserInfo?> GetAssociatedUserAsync(int commerceId);
    }
}
