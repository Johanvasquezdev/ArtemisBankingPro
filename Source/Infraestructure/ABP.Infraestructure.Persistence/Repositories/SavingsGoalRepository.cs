using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace ABP.Infraestructure.Persistence.Repositories
{
    public class SavingsGoalRepository(ArtemisBankingDbContext dbContext) : ABP.Infraestructure.Persistence.Repositories.Generic.GenericRepository<SavingsGoal>(dbContext), ISavingsGoalRepository
    {
        private readonly ArtemisBankingDbContext _dbContext = dbContext;
        public async Task<List<SavingsGoal>> GetBySavingsAccountIdAsync(int accountId)
        {
            return await _dbContext.SavingsGoals.Where(g => g.SavingsAccountId == accountId).ToListAsync();
        }
    }
}
