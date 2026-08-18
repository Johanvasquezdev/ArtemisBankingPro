using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;
using ABP.Infraestructure.identity.Context;

namespace ABP.Infraestructure.Persistence.Repositories
{
    public class SavingsAccountRepository(ArtemisBankingDbContext context, IdentityContext identityContext) : GenericRepository<SavingsAccount>(context), ISavingsAccountRepository
    {
        private readonly IdentityContext _identityContext = identityContext;
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

        public async Task<IEnumerable<SavingsAccount>> GetAllPagedAsync(int page, int pageSize, AccountStatus? status = null, AccountType? type = null)
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

            return await query.OrderByDescending(q => q.CreatedAt).Skip((page - 1) * pageSize)
                .Take(pageSize).ToListAsync();
        }

        public async Task<SavingsAccount?> GetByAccountNumberAsync(string accountNumber)
        {
            return await _dbSet.FirstOrDefaultAsync(a => a.AccountNumber == accountNumber);
        }

        public async Task<IEnumerable<SavingsAccount>> GetByClientCedulaAsync(string cedula, AccountStatus? status = null, AccountType? type = null)
        {
            var clientId = await _identityContext.Users.Where(u => u.Cedula == cedula).Select(u => u.Id).FirstOrDefaultAsync();

            if (clientId == null)
            {
                return [];
            }

            var query = _dbSet.AsNoTracking().Where(a => a.UserId == clientId);
            if (status.HasValue) 
            { 
                query = query.Where(a => a.Status == status.Value);
            }
            if (type.HasValue)
            {
                query = query.Where(a => a.Type == type.Value);
            }

            return await query.OrderByDescending(a => a.CreatedAt).ThenByDescending(a => a.CreatedAt).ToListAsync();
        }

        public async Task<SavingsAccount?> GetPrimaryAccountByClientIdAsync(string clientId)
        {
            return await _dbSet.FirstOrDefaultAsync(a => a.UserId == clientId && a.Type == AccountType.Primary && a.Status == AccountStatus.Active);
        }

        public async Task<int> GetTotalActiveAccountsCountAsync()
        {
            return await _dbSet.CountAsync(a => a.Status == AccountStatus.Active);
        }
    }
}
