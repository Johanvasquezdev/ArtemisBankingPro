using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.DTOs.SavingsAccount;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Domain.Enums;

namespace ABP.Core.Application.Interfaces.IServices
{
    public interface ISavingsAccountService
    {
        Task<SavingsAccountDto> GetByIdAsync(int id);
        Task<SavingsAccountDto?> GetByAccountNumberAsync(string accountNumber);
        Task<IEnumerable<SavingsAccountDto>> GetByClientIdAsync(string clientId);
        Task<SavingsAccountDto?> GetPrimaryAccountByClientIdAsync(string clientId);
        Task<PaginatedResult<SavingsAccountDto>> GetAllPagedAsync(int page, int pageSize = 20, AccountStatus? status = null, AccountType? type = null, string? cedula = null);

        Task<SavingsAccountDto> CreateAccountAsync(string clientId, string adminId, decimal initialAmount, AccountType type = AccountType.Primary);
        Task UpdateAsync(SavingsAccountDto dto);
        Task<bool> ChangeStatusAsync(int accountId, AccountStatus status);

        Task<bool> DepositAsync(string accountNumber, decimal amount);
        Task<bool> WithdrawAsync(string accountNumber, decimal amount);
        Task<bool> TransferAsync(string sourceAccountNumber, string destinationAccountNumber, decimal amount);

        Task<bool> AccountNumberExistsAsync(string accountNumber);
        Task<int> GetTotalActiveAccountsCountAsync();
        Task<bool> HasActiveAccountAsync(string clientId);
        Task<IEnumerable<TransactionDto>> GetTransactionsAsync(string accountNumber);
        Task AssignSecondaryAsync(AssignSavingsAccountDto dto);
        Task CancelAsync(string accountNumber);
    }
}
