using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.DTOs.Cashier;
using ABP.Core.Application.DTOs.Common;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.Transaction;

namespace ABP.Core.Application.Interfaces.IServices
{
    public interface ITransactionService
    {
        Task<TransactionDto> GetByIdAsync(int id);
        Task<IEnumerable<TransactionDto>> GetByAccountIdAsync(int savingsAccountId);

        // Client module operations
        Task<CommandResult> MakeExpressTransactionAsync(MakeExpressTransactionDto dto);
        Task<CommandResult> PayCreditCardAsync(PayCreditCardDto dto);
        Task<CommandResult> PayLoanAsync(PayLoanDto dto);
        Task<CommandResult> PayBeneficiaryAsync(PayBeneficiaryDto dto);
        Task<CommandResult> TransferOwnAccountsAsync(TransferOwnAccountsDto dto);
        Task<CommandResult> CashAdvanceAsync(CashAdvanceDto dto);

        Task<int> GetTodayTransactionsCountAsync();
        Task<int> GetTotalTransactionsCountAsync();
        Task<int> GetTodayPaymentsCountAsync();
        Task<int> GetTotalPaymentsCountAsync();
        Task DepositAsync(CashierDepositDto cashierDepositDto);
        Task WithdrawAsync(CashierWithdrawalDto dto);
        Task CashierPayCreditCardAsync(CashierPayCreditCardDto dto);
        Task CashierPayLoanAsync(CashierPayLoanDto Dto);
        Task CashierTransferAsync(CashierTransferDto dto);
    }
}
