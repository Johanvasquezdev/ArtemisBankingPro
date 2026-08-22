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
    public class ScheduledPaymentRepository(ArtemisBankingDbContext dbContext) : GenericRepository<ScheduledPayment>(dbContext), IScheduledPaymentRepository
    {
        private readonly ArtemisBankingDbContext _dbContext = dbContext;
        public async Task<List<ScheduledPayment>> GetBySavingsAccountIdAsync(int accountId)
        {
            return await _dbContext.ScheduledPayments.Where(p => p.SavingsAccountId == accountId).ToListAsync();
        }
        public async Task<List<ScheduledPayment>> GetActivePaymentsForDayAsync(int day)
        {
            return await _dbContext.ScheduledPayments.Where(p => p.IsActive && p.ExecutionDay == day).ToListAsync();
        }
    }
}