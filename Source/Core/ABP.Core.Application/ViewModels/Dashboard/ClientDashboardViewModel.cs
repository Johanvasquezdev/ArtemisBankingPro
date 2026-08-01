using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.DTOs.SavingsAccount;

namespace ABP.Core.Application.ViewModels.Dashboard
{
    public class ClientDashboardViewModel
    {
        public int TotalSavingsAccounts { get; set; }
        public int TotalCreditCards { get; set; }
        public int TotalLoans { get; set; }
        public IEnumerable<SavingsAccountDto> SavingsAccounts { get; set; } = [];
        public IEnumerable<CreditCardDto> CreditCards { get; set; } = [];
        public IEnumerable<LoanDto> Loans { get; set; } = [];
    }
}
