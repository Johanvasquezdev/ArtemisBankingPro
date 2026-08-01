using ABP.Core.Domain.Enums;

namespace ABP.Core.Domain.Entities
{
    public class Transaction
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public TransactionType Type { get; set; }
        // transaction detiny
        public string Beneficiary { get; set; } = string.Empty;
        //transaction origin
        public string Origin { get; set; } = string.Empty;
        public TransactionStatus Status { get; set; }

        // Navigation properties
        public int SavingAccountId { get; set; }
        public SavingsAccount SavingsAccount { get; set; } = null!;
        public string SourceAccountNumber { get; set; } = string.Empty;
        public string DestinationAccountNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; } = string.Empty;
    }
}
