namespace ABP.Core.Application.ViewModels.CreditCard
{
    public class CancelCreditCardViewModel
    {
        public int CardId { get; set; }
        public string LastFourDigits { get; set; } = string.Empty;
        public decimal AmountOwed { get; set; }
        public bool HasError { get; set; }
        public string Error { get; set; } = string.Empty;
    }
}
