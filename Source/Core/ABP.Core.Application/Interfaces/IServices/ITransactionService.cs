using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.DTOs.Cashier;
using ABP.Core.Application.DTOs.Transaction;

namespace ABP.Core.Application.Interfaces.IServices
{
    public interface ITransactionService
    {
        Task<TransactionDto> GetByIdAsync(int id);
        Task<IEnumerable<TransactionDto>> GetByAccountIdAsync(int savingsAccountId);

        Task<TransactionDto> TransferAsync(TransferDto dto);
        Task<TransactionDto> PayExpressAsync(PaymentDto dto);
        Task<TransactionDto> PayCreditCardAsync(PaymentDto dto);
        Task<TransactionDto> PayLoanAsync(PaymentDto dto);

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
