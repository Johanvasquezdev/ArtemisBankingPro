using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.DTOs.LoanInstallment;

namespace ABP.Core.Application.ViewModels.Account
{
    public class ClientLoanDetailViewModel
    {
        public IEnumerable<LoanInstallmentDto> Installments { get; set; } = new List<LoanInstallmentDto>();
        public decimal TotalPendingAmount { get; set; }
        public int TotalInstallments { get; set; }
        public int PaidInstallments { get; set; }
        public LoanDto Loan { get; set; } = new LoanDto();
    }
}
