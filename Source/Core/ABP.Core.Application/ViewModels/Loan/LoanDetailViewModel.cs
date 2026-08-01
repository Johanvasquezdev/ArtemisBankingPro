using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.DTOs.LoanInstallment;

namespace ABP.Core.Application.ViewModels.Loan
{
    public class LoanDetailViewModel
    {
        public LoanDto Loan { get; set; } = null!;
        public IEnumerable<LoanInstallmentDto> Installments { get; set; } = [];
        public decimal TotalPendingAmount { get; set; }
        public int PaidInstallments { get; set; }
        public int TotalInstallments { get; set; }
    }
}
