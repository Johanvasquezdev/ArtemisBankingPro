namespace ABP.Core.Application.ViewModels.Cashier
{
    public class CashierWithdrawalViewModel
    {
        public string AccountNumber { get; set; } = string.Empty;
        public bool HasError { get; set; }
        public string Error { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string AccountHolderName { get; set; } = string.Empty;
    }
}
