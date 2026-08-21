namespace ABP.Core.Application.DTOs.Cashier
{
    public class CashierPayLoanDto
    {
        public string SourceAccountNumber { get; set; } = string.Empty;
        public string LoanNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PerformedByUserId { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty;
    }
}
