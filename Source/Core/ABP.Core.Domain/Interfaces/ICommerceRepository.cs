using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces.IGenerics;

namespace ABP.Core.Domain.Interfaces
{
    public interface ICommerceRepository : IGenericRepository<Commerce>
    {
        Task<Commerce?> GetByIdWithUserAsync(int commerceId);
        Task<string?> GetActiveUserIdAsync(int commerceId);
        Task<IEnumerable<Commerce>> GetAllPagedAsync(int? page = null, int? pageSize = null);
        Task<bool> CommerceHasActiveUserAsync(int commerceId);
    }
}
