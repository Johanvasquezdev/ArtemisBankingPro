using ABP.Core.Domain.Enums;

namespace ABP.Core.Application.ViewModels.Client
{
    public class ClientLoanItemViewModel
    {
        public int Id { get; set; }
        public string LoanNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal PendingAmount { get; set; }
        public int TotalInstallments { get; set; }
        public int PaidInstallments { get; set; }
        public bool IsOnTime { get; set; }
        public LoanStatus Status { get; set; }
        public DateTime? NextDueDate { get; set; }
    }
}
