using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces.IGenerics;

namespace ABP.Core.Domain.Interfaces
{
    public interface IIdempotencyRepository : IGenericRepository<IdempotencyRecord>
    {
        Task<IdempotencyRecord?> GetAsync(string operation, string key, string actorUserId);
    }
}
