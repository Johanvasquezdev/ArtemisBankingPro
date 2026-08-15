namespace ABP.Core.Application.DTOs.Transaction
{
    public class PayCreditCardDto
    {
        public string ClientId { get; set; } = string.Empty;
        public string SourceAccountNumber { get; set; } = string.Empty;
        public string CreditCardNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
    }
}
