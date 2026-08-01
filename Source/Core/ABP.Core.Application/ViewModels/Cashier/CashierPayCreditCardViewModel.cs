namespace ABP.Core.Application.ViewModels.Cashier
{
    public class CashierPayCreditCardViewModel
    {
        public bool HasError { get; set; }
        public string Error { get; set; } = string.Empty;
        public string SourceAccountNumber { get; set; } = string.Empty;
        public string CardNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string CardHolderName { get; set; } = string.Empty;
    }
}
