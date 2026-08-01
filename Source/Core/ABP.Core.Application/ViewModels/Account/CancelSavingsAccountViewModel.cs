namespace ABP.Core.Application.ViewModels.Account
{
    public class CancelSavingsAccountViewModel
    {
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public bool HasError { get; set; }
        public string Error { get; set; } = string.Empty;
    }
}
