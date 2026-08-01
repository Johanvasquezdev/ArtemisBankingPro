namespace ABP.Core.Application.DTOs.Cashier
{
    public class CashierDepositDto
    {
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
