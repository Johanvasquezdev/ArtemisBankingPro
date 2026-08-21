using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infraestructure.Persistence.Repositories
{
    public class LoanInstallmentRepository(ArtemisBankingDbContext context) : GenericRepository<LoanInstallment>(context), ILoanInstallmentRepository
    {
        public async Task<IEnumerable<LoanInstallment>> GetByLoanIdAsync(int loanId)
        {
            return await _dbSet.AsNoTracking().Where(li => li.LoanId == loanId).OrderBy(li => li.DueDate).ToListAsync();
        }

        public async Task<IEnumerable<LoanInstallment>> GetByLoanIdsAsync(IEnumerable<int> loanIds)
        {
            var ids = loanIds.Distinct().ToArray();
            if (ids.Length == 0) return [];

            return await _dbSet.AsNoTracking()
                .Where(li => ids.Contains(li.LoanId))
                .OrderBy(li => li.LoanId)
                .ThenBy(li => li.DueDate)
                .ToListAsync();
        }

        public async Task<LoanInstallment?> GetFirstPendingInstallmentAsync(int loanId)
        {
            // first pending installment is the one with the earliest due date that is not paid yet
            return await _dbSet.Where(li => li.LoanId == loanId && li.Status != InstallmentStatus.Paid)
                .OrderBy(li => li.DueDate).FirstOrDefaultAsync();
        }

        public async Task<IEnumerable<LoanInstallment>> GetFutureUnpaidInstallmentsAsync(int loanId)
        {
            return await _dbSet.Where(li => li.LoanId == loanId && li.Status != InstallmentStatus.Paid && li.DueDate > DateTime.UtcNow)
                .OrderBy(li => li.InstallmentNumber).ToListAsync();
        }

        public async Task<IEnumerable<LoanInstallment>> GetOverdueInstallmentsAsync()
        {
            return await _dbSet.Where(li => li.Status != InstallmentStatus.Paid && li.DueDate < DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<IEnumerable<LoanInstallment>> GetOverdueInstallmentsByLoanIdsAsync(IEnumerable<int> loanIds)
        {
            var ids = loanIds.Distinct().ToArray();
            if (ids.Length == 0) return [];

            return await _dbSet
                .Where(li => ids.Contains(li.LoanId)
                    && li.Status != InstallmentStatus.Paid
                    && li.DueDate < DateTime.UtcNow)
                .ToListAsync();
        }

        public async Task<int> GetPaidInstallmentsCountAsync(int loanId)
        {
            return await _dbSet.CountAsync(li => li.LoanId == loanId && li.Status == InstallmentStatus.Paid);
        }

        public async Task<decimal> GetPendingAmountByLoanIdAsync(int loanId)
        {
            return await _dbSet.Where(li => li.LoanId == loanId && li.Status != InstallmentStatus.Paid)
                .SumAsync(li => li.InstallmentAmount - li.AmountPaid);
        }
    }
}
