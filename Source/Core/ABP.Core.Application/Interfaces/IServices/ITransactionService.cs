using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.DTOs.Cashier;
using ABP.Core.Application.DTOs.Common;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.Transaction;

namespace ABP.Core.Application.Interfaces.IServices
{
// Compatibility contract for legacy callers. New features should depend on
// the narrow query/client/cashier contracts instead.
public interface ITransactionService :
    ITransactionQueryService,
    IClientTransactionService,
    ICashierTransactionService
{
}
}
