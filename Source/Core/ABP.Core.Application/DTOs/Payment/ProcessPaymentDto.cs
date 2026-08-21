namespace ABP.Core.Application.DTOs.Payment
{
    public class ProcessPaymentDto
    {
        public string CardNumber { get; set; } = string.Empty;
        public string MonthExpirationCard { get; set; } = string.Empty;
        public string YearExpirationCard { get; set; } = string.Empty;
        public string CVC { get; set; } = string.Empty;
        public decimal TransactionAmount { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
    }
}
