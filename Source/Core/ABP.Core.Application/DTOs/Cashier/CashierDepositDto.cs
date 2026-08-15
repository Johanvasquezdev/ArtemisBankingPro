namespace ABP.Core.Application.DTOs.Cashier
{
    public class CashierDepositDto
    {
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string PerformedByUserId { get; set; } = string.Empty;
        public string IdempotencyKey { get; set; } = string.Empty;
    }
}
