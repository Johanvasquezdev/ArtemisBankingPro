using ABP.Core.Domain.Enums;

namespace ABP.Core.Application.DTOs.Payment
{
    public class PaymentTransactionDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public TransactionStatus Status { get; set; }
    }
}
