using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infraestructure.Persistence.Repositories
{
    public class CommerceRepository(ArtemisBankingDbContext context) : GenericRepository<Commerce>(context), ICommerceRepository
    {
        public async Task<IEnumerable<Commerce>> GetAllPagedAsync(int? page = null, int? pageSize = null, bool? isActive = null)
        {
            var query = _dbSet.AsNoTracking().AsQueryable();
            if (isActive.HasValue)
                query = query.Where(c => c.IsActive == isActive.Value);

            query = query.OrderByDescending(c => c.CreatedAt);

            if (page.HasValue && pageSize.HasValue)
            {
                return await query.Skip((page.Value - 1) * pageSize.Value).Take(pageSize.Value).ToListAsync();
            }
            return await query.ToListAsync();
        }

        public async Task<Commerce?> GetByIdWithUserAsync(int commerceId)
        {
            return await _dbSet.FirstOrDefaultAsync(c => c.Id == commerceId);
        }

        public Task<bool> ExistsByRncAsync(string rnc, int? excludingId = null) =>
            _dbSet.AnyAsync(c => c.Rnc == rnc && (!excludingId.HasValue || c.Id != excludingId.Value));

        public Task<bool> ExistsByEmailAsync(string email, int? excludingId = null) =>
            _dbSet.AnyAsync(c => c.Email == email && (!excludingId.HasValue || c.Id != excludingId.Value));

    }
}
