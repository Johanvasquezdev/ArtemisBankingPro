namespace ABP.Core.Application.ViewModels.Cashier
{
    public class CashierPayLoanViewModel
    {
        public string LoanNumber { get; set; } = string.Empty;
        public string SourceAccountNumber { get; set; } = string.Empty;
        public bool HasError { get; set; }
        public string Error { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string LoanHolderName { get; set; } = string.Empty;
    }
}
