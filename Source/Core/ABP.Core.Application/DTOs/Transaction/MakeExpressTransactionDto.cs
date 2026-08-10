namespace ABP.Core.Application.DTOs.Transaction
{
    public class MakeExpressTransactionDto
    {
        public string ClientId { get; set; } = string.Empty;
        public string SourceAccountNumber { get; set; } = string.Empty;
        public string DestinationAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
