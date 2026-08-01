namespace ABP.Core.Application.DTOs.CreditCard
{
    public class CashAdvanceDto
    {
        public int CreditCardId { get; set; }
        public int SavingsAccountId { get; set; }
        public decimal Amount { get; set; }
    }
}
