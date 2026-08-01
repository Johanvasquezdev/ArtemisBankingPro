namespace ABP.Core.Application.DTOs.Payment
{
    public class PaymentResultDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? TransactionId { get; set; }
        public decimal? NewBalance { get; set; }
    }
}
