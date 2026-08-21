namespace ABP.Core.Application.DTOs.CreditCard
{
    public class CashAdvanceDto
    {
        public required string ClientId { get; set; }
        public int CreditCardId { get; set; }
        public int SavingsAccountId { get; set; }
        public decimal Amount { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
    }
}
