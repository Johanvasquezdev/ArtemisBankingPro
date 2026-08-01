using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infraestructure.Persistence.Repositories
{
    public class CreditCardConsumptionRepository(ArtemisBankDbContext context) : GenericRepository<CreditCardConsumption>(context), ICreditCardConsumptionRepository
    {
        public async Task<IEnumerable<CreditCardConsumption>> GetByCardIdAsync(int creditCardId)
        {
            return await _dbSet.Where(c => c.CreditCardId == creditCardId).OrderByDescending(c => c.TransactionDate).ToListAsync();
        }
    }
}
