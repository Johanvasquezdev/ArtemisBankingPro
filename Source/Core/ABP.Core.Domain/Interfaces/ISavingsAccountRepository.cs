using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces.IGenerics;

namespace ABP.Core.Domain.Interfaces
{
    public interface ISavingsAccountRepository : IGenericRepository<SavingsAccount>
    {
        Task<SavingsAccount?> GetByAccountNumberAsync(string accountNumber);
        Task<SavingsAccount?> GetPrimaryAccountByClientIdAsync(string clientId);
        Task<IEnumerable<SavingsAccount>> GetActiveAccountsByClientIdAsync(string customerId);
        Task<IEnumerable<SavingsAccount>> GetAllAccountByClienteIdAsync(string clientId);
        Task<bool> AccountOrLoanNumberExistsAsync(string number);
        Task<IEnumerable<SavingsAccount>> GetAllPagedAsync(int page, int pageSize, AccountStatus? status = null, AccountType? type = null, string? userId = null);
        Task<int> GetTotalActiveAccountsCountAsync(AccountStatus? status = null, AccountType? type = null, string? userId = null);
    }
}
