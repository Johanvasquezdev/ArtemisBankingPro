using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces.IGenerics;

namespace ABP.Core.Domain.Interfaces
{
    public interface ITransactionRepository : IGenericRepository<Transaction>
    {
        Task<int> GetTodayTransactionsByUserIdCountAsync(string userId);
        Task<int> GetTodayPaymentsByUserIdCountAsync(string userId);
        Task<int> GetTodayDepositsByUserIdCountAsync(string userId);
        Task<int> GetTodayWithdrawalsByUserIdCountAsync(string userId);
        Task<decimal> GetTodayDepositsAmountByUserIdAsync(string userId);
        Task<decimal> GetTodayWithdrawalsAmountByUserIdAsync(string userId);
        Task<int> GetTodayPaymentsCountAsync();
        Task<int> GetTotalPaymentsCountAsync();
        Task<int> GetTodayTransactionsCountAsync();
        Task<int> GetTotalTransactionsCountAsync();
        Task<IEnumerable<Transaction>> GetByAccountIdAsync(int savingsAccountId);
        Task<IEnumerable<Transaction>> GetByAccountIdsAsync(IEnumerable<int> savingsAccountIds);
        Task<IEnumerable<Transaction>> GetByAccountNumberAsync(string accountNumber);
        Task<IEnumerable<Transaction>> GetRecentAsync(int take);
        Task<IEnumerable<Transaction>> GetRecentByPerformerIdAsync(string performerId, int take);
    }
}
