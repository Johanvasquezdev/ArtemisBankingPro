using ABP.Core.Domain.Enums;

namespace ABP.Core.Application.DTOs.LoanInstallment
{
    public class LoanInstallmentDto
    {
        public int Id { get; set; }
        public DateTime DueDate { get; set; }
        public decimal InstallmentAmount { get; set; }
        public decimal AmountPaid { get; set; }
        public decimal PrincipalPortion { get; set; }
        public decimal InterestPortion { get; set; }
        public InstallmentStatus Status { get; set; }
        public bool IsOverdue { get; set; }
        public int InstallmentNumber { get; set; }
        public int LoanId { get; set; }
    }
}
