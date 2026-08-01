using ABP.Core.Application.DTOs.SavingsAccount;
using ABP.Core.Application.DTOs.Transaction;

namespace ABP.Core.Application.ViewModels.Account
{
    public class SavingsAccountDetailViewModel
    {
        public SavingsAccountDto Account {  get; set; } = new SavingsAccountDto();
        public IEnumerable<TransactionDto> Transactions { get; set; } = [];
    }
}
