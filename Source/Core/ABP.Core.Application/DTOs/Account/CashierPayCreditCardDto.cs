namespace ABP.Core.Application.DTOs.Account
{
    public class CashierPayCreditCardDto
    {
        public string SourceAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CardNumber { get; set; } = string.Empty;
    }
}
