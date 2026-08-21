using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infraestructure.Persistence.Repositories
{
    public class CreditCardConsumptionRepository(ArtemisBankingDbContext context) : GenericRepository<CreditCardConsumption>(context), ICreditCardConsumptionRepository
    {
        public async Task<IEnumerable<CreditCardConsumption>> GetByCardIdAsync(int creditCardId)
        {
            return await _dbSet.Where(c => c.CreditCardId == creditCardId).OrderByDescending(c => c.TransactionDate).ToListAsync();
        }

        public async Task<IEnumerable<CreditCardConsumption>> GetByCommerceIdAsync(int commerceId)
        {
            return await _dbSet.Where(c => c.CommerceId == commerceId)
                .OrderByDescending(c => c.TransactionDate)
                .ToListAsync();
        }

        public async Task<(IEnumerable<CreditCardConsumption> Items, int TotalCount)> GetByCommerceIdPagedAsync(int commerceId, int page, int pageSize)
        {
            var query = _dbSet.Where(c => c.CommerceId == commerceId);
            int totalCount = await query.CountAsync();
            
            var items = await query
                .OrderByDescending(c => c.TransactionDate)
                .Skip((page - 1) * pageSize)
                .Take(pageSize)
                .ToListAsync();
                
            return (items, totalCount);
        }
    }
}
