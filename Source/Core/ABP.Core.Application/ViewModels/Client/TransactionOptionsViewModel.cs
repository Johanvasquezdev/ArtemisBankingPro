using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.DTOs.SavingsAccount;

namespace ABP.Core.Application.ViewModels.Client
{
    public class TransactionOptionsViewModel
    {
        public IReadOnlyList<SavingsAccountDto> Accounts { get; set; } = [];
        public IReadOnlyList<CreditCardDto> CreditCards { get; set; } = [];
        public IReadOnlyList<LoanDto> Loans { get; set; } = [];
        public IReadOnlyList<BeneficiaryListItemViewModel> Beneficiaries { get; set; } = [];
    }
}
