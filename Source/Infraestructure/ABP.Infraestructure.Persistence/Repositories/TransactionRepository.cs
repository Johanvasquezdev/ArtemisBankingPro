using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infraestructure.Persistence.Repositories
{
    public class TransactionRepository(ArtemisBankDbContext context) : GenericRepository<Transaction>(context), ITransactionRepository
    {
        public async Task<IEnumerable<Transaction>> GetByAccountIdAsync(int savingsAccountId)
        {
            return await _dbSet.Where(t => t.SavingAccountId == savingsAccountId)
                .OrderByDescending(t => t.TransactionDate).ToListAsync();
        }

        public async Task<int> GetTodayDepositsByUserIdCountAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;
            return await _dbSet.CountAsync(t => t.TransactionDate.Date == today && t.Type == TransactionType.Credit 
            && t.SavingsAccount.UserId == userId && t.Origin == "Deposit");
        }

        public async Task<int> GetTodayPaymentsByUserIdCountAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;
            return await _dbSet.CountAsync(t => t.TransactionDate.Date == today && t.Type == TransactionType.Debit && t.SavingsAccount.UserId == userId
            && (t.Beneficiary.Length == 16 || t.Beneficiary.Length == 9));
        }

        public async Task<int> GetTodayPaymentsCountAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _dbSet.CountAsync(t => t.TransactionDate.Date == today && t.Type == TransactionType.Debit 
            && (t.Beneficiary.Length == 16 || t.Beneficiary.Length == 9));
        }

        public async Task<int> GetTodayTransactionsByUserIdCountAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;
            return await _dbSet.CountAsync(t => t.TransactionDate.Date == today && t.SavingsAccount.UserId == userId);
        }

        public async Task<int> GetTodayTransactionsCountAsync()
        {
            var today = DateTime.UtcNow.Date;
            return await _dbSet.CountAsync(t => t.TransactionDate.Date == today);
        }

        public async Task<int> GetTodayWithdrawalsByUserIdCountAsync(string userId)
        {
            var today = DateTime.UtcNow.Date;
            return await _dbSet.CountAsync(t => t.TransactionDate.Date == today && t.Type == TransactionType.Debit 
            && t.SavingsAccount.UserId == userId && t.Beneficiary == "Withdrawal");
        }

        public async Task<int> GetTotalPaymentsCountAsync()
        {
            return await _dbSet.CountAsync(t => t.Type == TransactionType.Debit
                    && (t.Beneficiary.Length == 16 || t.Beneficiary.Length == 9));
        }

        public async Task<int> GetTotalTransactionsCountAsync()
        {
            return await _dbSet.CountAsync();
        }
    }
}
