namespace ABP.Core.Application.DTOs.Transaction
{
    public class PaymentDto
    {
        public string SourceAccountNumber { get; set; } = string.Empty;
        public string DestinationAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
