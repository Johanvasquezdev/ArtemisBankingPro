namespace ABP.Core.Application.DTOs.Transaction
{
    public class PayLoanDto
    {
        public string ClientId { get; set; } = string.Empty;
        public string SourceAccountNumber { get; set; } = string.Empty;
        public string LoanNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
