using ABP.Core.Application.DTOs.Common;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.Transaction;

namespace ABP.Core.Application.Interfaces.IServices;

public interface IClientTransactionService
{
    Task<CommandResult> MakeExpressTransactionAsync(MakeExpressTransactionDto dto);
    Task<CommandResult> PayCreditCardAsync(PayCreditCardDto dto);
    Task<CommandResult> PayLoanAsync(PayLoanDto dto);
    Task<CommandResult> PayBeneficiaryAsync(PayBeneficiaryDto dto);
    Task<CommandResult> TransferOwnAccountsAsync(TransferOwnAccountsDto dto);
    Task<CommandResult> CashAdvanceAsync(CashAdvanceDto dto);
}
