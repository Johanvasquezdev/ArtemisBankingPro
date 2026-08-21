namespace ABP.Core.Application.DTOs.Transaction
{
    public class TransferOwnAccountsDto
    {
        public string ClientId { get; set; } = string.Empty;
        public string SourceAccountNumber { get; set; } = string.Empty;
        public string DestinationAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
    }
}
