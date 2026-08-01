namespace ABP.Core.Application.DTOs.Cashier
{
    public class CashierTransferDto
    {
        public string SourceAccountNumber { get; set; } = string.Empty;
        public string DestinationAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
