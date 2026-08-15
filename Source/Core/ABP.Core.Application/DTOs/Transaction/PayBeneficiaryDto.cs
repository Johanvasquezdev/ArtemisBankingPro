namespace ABP.Core.Application.DTOs.Transaction
{
    public class PayBeneficiaryDto
    {
        public string ClientId { get; set; } = string.Empty;
        public int BeneficiaryId { get; set; }
        public string SourceAccountNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public string IdempotencyKey { get; set; } = string.Empty;
    }
}
