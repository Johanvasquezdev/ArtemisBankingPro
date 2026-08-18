using ABP.Core.Application.DTOs.Cashier;

namespace ABP.Core.Application.Interfaces.IServices;

public interface ICashierTransactionService
{
    Task DepositAsync(CashierDepositDto dto);
    Task WithdrawAsync(CashierWithdrawalDto dto);
    Task CashierPayCreditCardAsync(CashierPayCreditCardDto dto);
    Task CashierPayLoanAsync(CashierPayLoanDto dto);
    Task CashierTransferAsync(CashierTransferDto dto);
}
