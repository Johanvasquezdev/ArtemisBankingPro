using ABP.Core.Domain.Enums;

namespace ABP.Core.Application.DTOs.Loan
{
    public class LoanDto
    {
        public int Id { get; set; }
        public string LoanNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public int TotalInstallments { get; set; }
        public int PaidInstallments { get; set; }
        public decimal PendingAmount { get; set; }
        public int TermInMonths { get; set; }
        public LoanStatus Status { get; set; }
        public bool IsOnTime { get; set; }
        public string ClientFullName { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
    }
}
