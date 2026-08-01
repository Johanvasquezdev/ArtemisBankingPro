using ABP.Core.Domain.Enums;

namespace ABP.Core.Domain.Entities
{
    public class Loan
    {
        public int Id { get; set; }
        public string LoanNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal AnualInterestRate { get; set; }
        public int TermInMonths { get; set; }
        public LoanStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<LoanInstallment> Installments { get; set; } = [];

        // foreign keys to user
        public string ClientId { get; set; } = string.Empty;
        public string AssignedByAdminId { get; set; } = string.Empty;
    }
}
