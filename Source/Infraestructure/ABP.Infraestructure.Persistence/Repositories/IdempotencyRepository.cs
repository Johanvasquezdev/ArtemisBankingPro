using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infraestructure.Persistence.Repositories
{
    public class IdempotencyRepository(ArtemisBankDbContext context)
        : GenericRepository<IdempotencyRecord>(context), IIdempotencyRepository
    {
        public Task<IdempotencyRecord?> GetAsync(string operation, string key, string actorUserId)
            => _dbSet.AsNoTracking().SingleOrDefaultAsync(x =>
                x.Operation == operation && x.Key == key && x.ActorUserId == actorUserId);
    }
}
