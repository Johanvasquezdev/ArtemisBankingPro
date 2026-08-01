namespace ABP.Core.Application.DTOs.Account
{
    public class CashierWithdrawalDto
    {
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
