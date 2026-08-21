using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infraestructure.Persistence.Repositories
{
    public class SavingsAccountRepository(ArtemisBankingDbContext context) : GenericRepository<SavingsAccount>(context), ISavingsAccountRepository
    {
        public async Task<bool> AccountOrLoanNumberExistsAsync(string number)
        {
            bool existsAsAccount = await _dbSet.AnyAsync(a => a.AccountNumber == number);
            bool existsAsLoan = await _context.Loans.AnyAsync(l => l.LoanNumber == number);
            return existsAsAccount || existsAsLoan;
        }

        public async Task<IEnumerable<SavingsAccount>> GetActiveAccountsByClientIdAsync(string customerId)
        {
            return await _dbSet.Where(a => a.UserId == customerId && a.Status == AccountStatus.Active)
                .OrderByDescending(a => a.Type == AccountType.Primary)
                .ThenByDescending(a => a.Balance)
                .ToListAsync();
        }

        public async Task<IEnumerable<SavingsAccount>> GetAllAccountByClienteIdAsync(string clientId)
        {
            return await _dbSet.Where(c => c.UserId == clientId)
                .OrderByDescending(c => c.Status == AccountStatus.Active)
                .ThenByDescending(c => c.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<SavingsAccount>> GetAllPagedAsync(int page, int pageSize, AccountStatus? status = null, AccountType? type = null, string? userId = null)
        {
            var query = _dbSet.AsNoTracking();

            if (status.HasValue)
            {
                query = query.Where(a => a.Status == status.Value);
            }
            if (type.HasValue) 
            {
                query = query.Where(a => a.Type == type.Value);
            }
            if (!string.IsNullOrEmpty(userId))
            {
                query = query.Where(a => a.UserId == userId);
            }

            return await query.OrderByDescending(q => q.CreatedAt).Skip((page - 1) * pageSize)
                .Take(pageSize).ToListAsync();
        }

        public async Task<SavingsAccount?> GetByAccountNumberAsync(string accountNumber)
        {
            return await _dbSet.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        }

        public async Task<SavingsAccount?> GetPrimaryAccountByClientIdAsync(string clientId)
        {
            return await _dbSet.FirstOrDefaultAsync(a => a.UserId == clientId && a.Type == AccountType.Primary && a.Status == AccountStatus.Active);
        }

        public async Task<int> GetTotalActiveAccountsCountAsync(AccountStatus? status = null, AccountType? type = null, string? userId = null)
        {
            var query = _dbSet.AsNoTracking();
            if (status.HasValue) query = query.Where(a => a.Status == status.Value);
            if (type.HasValue) query = query.Where(a => a.Type == type.Value);
            if (!string.IsNullOrEmpty(userId)) query = query.Where(a => a.UserId == userId);

            return await query.CountAsync();
        }
    }
}
