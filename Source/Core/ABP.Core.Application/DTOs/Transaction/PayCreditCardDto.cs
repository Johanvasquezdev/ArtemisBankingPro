namespace ABP.Core.Application.DTOs.Transaction
{
    public class PayCreditCardDto
    {
        public string ClientId { get; set; } = string.Empty;
        public string SourceAccountNumber { get; set; } = string.Empty;
        public int CreditCardId { get; set; }
        public decimal Amount { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
    }
}
