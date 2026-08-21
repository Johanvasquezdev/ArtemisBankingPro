using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infraestructure.Persistence.Repositories
{
    public class TransactionRepository(ArtemisBankingDbContext context) : GenericRepository<Transaction>(context), ITransactionRepository
    {
        public override async Task AddAsync(Transaction entity)
        {
            if (string.IsNullOrWhiteSpace(entity.PerformedByUserId))
                throw new InvalidOperationException("No se puede guardar una transacción sin PerformedByUserId.");
            
            await base.AddAsync(entity);
        }

        public override async Task AddWithoutSaveAsync(Transaction entity)
        {
            if (string.IsNullOrWhiteSpace(entity.PerformedByUserId))
                throw new InvalidOperationException("No se puede guardar una transacción sin PerformedByUserId.");
                
            await base.AddWithoutSaveAsync(entity);
        }

        public async Task<IEnumerable<Transaction>> GetByAccountIdAsync(int savingsAccountId)
        {
            return await _dbSet.AsNoTracking()
                .Where(t => t.SavingAccountId == savingsAccountId)
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetByAccountIdsAsync(IEnumerable<int> savingsAccountIds)
        {
            var ids = savingsAccountIds.Distinct().ToArray();
            if (ids.Length == 0) return [];

            return await _dbSet.AsNoTracking()
                .Where(t => ids.Contains(t.SavingAccountId))
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetByAccountNumberAsync(string accountNumber)
        {
            return await _dbSet.AsNoTracking()
                .Where(t => t.SourceAccountNumber == accountNumber || t.DestinationAccountNumber == accountNumber)
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedAt)
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetRecentAsync(int take)
        {
            return await _dbSet.AsNoTracking()
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedAt)
                .Take(Math.Clamp(take, 1, 500))
                .ToListAsync();
        }

        public async Task<IEnumerable<Transaction>> GetRecentByPerformerIdAsync(string performerId, int take)
        {
            return await _dbSet.AsNoTracking()
                .Where(t => t.PerformedByUserId == performerId)
                .OrderByDescending(t => t.TransactionDate)
                .ThenByDescending(t => t.CreatedAt)
                .Take(Math.Clamp(take, 1, 100))
                .ToListAsync();
        }

        public async Task<int> GetTodayDepositsByUserIdCountAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;
            return await _dbSet.CountAsync(t => t.TransactionDate.Date == today && t.Type == TransactionType.Credit
            && t.PerformedByUserId == userId && (t.Origin == "Deposit" || t.Origin == "CAJERO"));
        }

        public async Task<int> GetTodayPaymentsByUserIdCountAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;
            return await _dbSet.CountAsync(t => t.TransactionDate.Date == today && t.Type == TransactionType.Payment && t.Status == TransactionStatus.Approved && t.PerformedByUserId == userId);
        }

        public async Task<int> GetTodayPaymentsCountAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            return await _dbSet.CountAsync(t => t.TransactionDate >= today && t.TransactionDate < tomorrow && t.Type == TransactionType.Payment && t.Status == TransactionStatus.Approved);
        }

        public async Task<int> GetTodayTransactionsByUserIdCountAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;
            return await _dbSet.CountAsync(t => t.TransactionDate.Date == today && t.PerformedByUserId == userId);
        }

        public async Task<int> GetTodayTransactionsCountAsync()
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            return await _dbSet.CountAsync(t => t.TransactionDate >= today && t.TransactionDate < tomorrow);
        }

        public async Task<int> GetTodayWithdrawalsByUserIdCountAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;
            return await _dbSet.CountAsync(t => t.TransactionDate.Date == today && t.Type == TransactionType.Debit
            && t.PerformedByUserId == userId && (t.Beneficiary == "Withdrawal" || t.Beneficiary == "CAJERO"));
        }

        public async Task<decimal> GetTodayDepositsAmountByUserIdAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            return await _dbSet.Where(t => t.TransactionDate >= today && t.TransactionDate < tomorrow
                && t.Type == TransactionType.Credit && t.PerformedByUserId == userId
                && (t.Origin == "Deposit" || t.Origin == "CAJERO")).SumAsync(t => (decimal?)t.Amount) ?? 0m;
        }

        public async Task<decimal> GetTodayWithdrawalsAmountByUserIdAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;
            var tomorrow = today.AddDays(1);
            return await _dbSet.Where(t => t.TransactionDate >= today && t.TransactionDate < tomorrow
                && t.Type == TransactionType.Debit && t.PerformedByUserId == userId
                && (t.Beneficiary == "Withdrawal" || t.Beneficiary == "CAJERO"))
                .SumAsync(t => (decimal?)t.Amount) ?? 0m;
        }

        public async Task<int> GetTotalPaymentsCountAsync()
        {
            return await _dbSet.CountAsync(t => t.Type == TransactionType.Payment && t.Status == TransactionStatus.Approved);
        }

        public async Task<int> GetTotalTransactionsCountAsync()
        {
            return await _dbSet.CountAsync();
        }
    }
}
