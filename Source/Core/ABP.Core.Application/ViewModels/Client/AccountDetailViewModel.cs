using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Domain.Enums;

namespace ABP.Core.Application.ViewModels.Client
{
    public class AccountDetailViewModel
    {
        public int Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public AccountType Type { get; set; }
        public AccountStatus Status { get; set; }
        public string OwnerFullName { get; set; } = string.Empty;

        public DateTime? DateFrom { get; set; }
        public DateTime? DateTo { get; set; }

        public IReadOnlyList<TransactionDto> Transactions { get; set; } = [];
    }
}
