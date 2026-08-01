using ABP.Core.Domain.Enums;

namespace ABP.Core.Domain.Entities
{
    public class LoanInstallment
    {
        public int Id { get; set; }
        public DateTime DueDate { get; set; }
        public decimal InstallmentAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public InstallmentStatus Status { get; set; }
        // Updated daily by the Hangfire/Quartz job
        public bool IsOverdue { get; set; }
        public int InstallmentNumber { get; set; }
        // Foreign key to Loan
        public int LoanId { get; set; }
        public Loan Loan { get; set; } = null!;
    }
}
