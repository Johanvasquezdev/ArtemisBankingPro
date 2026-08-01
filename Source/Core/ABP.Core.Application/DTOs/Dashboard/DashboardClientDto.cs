using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.DTOs.SavingsAccount;

namespace ABP.Core.Application.DTOs.Dashboard
{
    public class DashboardClientDto
    {
        public int TotalSavingsAccounts { get; set; }
        public int TotalCreditCards { get; set; }
        public int TotalLoans { get; set; }
        public IEnumerable<SavingsAccountDto> SavingsAccounts { get; set; } = [];
        public IEnumerable<CreditCardDto> CreditCards { get; set; } = [];
        public IEnumerable<LoanDto> Loans { get; set; } = [];
    }
}
