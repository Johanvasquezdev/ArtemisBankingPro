using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infraestructure.Persistence.Repositories
{
    public class BeneficiaryRepository(ArtemisBankDbContext context) : GenericRepository<Beneficiary>(context), IBeneficiaryRepository
    {
        public async Task<bool> BeneficiaryExistForOwnerAsync(string ownerId, string accountNumber)
        {
            return await _dbSet.AnyAsync(b => b.OwnerId == ownerId && b.AccountNumber == accountNumber);
        }

        public async Task<IEnumerable<Beneficiary>> GetByOwnerAccountIdAsync(string userId)
        {
            return await _dbSet.Where(b => b.OwnerId == userId).OrderBy(b => b.FirstName).ToListAsync();
        }
    }
}
