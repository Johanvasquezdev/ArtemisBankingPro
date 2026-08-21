using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.SavingsAccount;
using ABP.Core.Application.DTOs.Transaction;

namespace ABP.Core.Application.ViewModels.Client
{
    public class ClientHomeViewModel
    {
        public string ClientFullName { get; set; } = string.Empty;
        public int TotalAccounts { get; set; }
        public int TotalCreditCards { get; set; }
        public int TotalLoans { get; set; }
        public IReadOnlyList<SavingsAccountDto> Accounts { get; set; } = [];
        public IReadOnlyList<CreditCardDto> CreditCards { get; set; } = [];
        public IReadOnlyList<ClientLoanItemViewModel> Loans { get; set; } = [];
        public IReadOnlyList<TransactionDto> RecentTransactions { get; set; } = [];
        public int OverdueInstallmentsCount { get; set; }
        public bool HasDelinquentLoans { get; set; }
    }
}
