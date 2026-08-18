using ABP.Core.Application.DTOs.Transaction;

namespace ABP.Core.Application.Interfaces.IServices;

public interface ITransactionQueryService
{
    Task<TransactionDto> GetByIdAsync(int id);
    Task<IEnumerable<TransactionDto>> GetByAccountIdAsync(int savingsAccountId);
    Task<IEnumerable<TransactionDto>> GetByAccountIdsAsync(IEnumerable<int> savingsAccountIds);
    Task<IEnumerable<TransactionDto>> GetHistoryAsync(int take = 100);
    Task<int> GetTodayTransactionsCountAsync();
    Task<int> GetTotalTransactionsCountAsync();
    Task<int> GetTodayPaymentsCountAsync();
    Task<int> GetTotalPaymentsCountAsync();
}
