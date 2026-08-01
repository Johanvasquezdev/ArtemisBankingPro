using ABP.Core.Domain.Enums;

namespace ABP.Core.Application.DTOs.Transaction
{
    public class TransactionDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public TransactionType Type { get; set; }
        public string Beneficiary { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        public TransactionStatus Status { get; set; }
        public int SavingAccountId { get; set; }
        public string Description { get; set; } = string.Empty; 
    }
}
