using ABP.Core.Domain.Enums;

namespace ABP.Core.Application.DTOs.Transaction
{
    public class TransactionEntry
    {
        public decimal Amount { get; init; }
        public TransactionType Type { get; init; }
        public string Origin { get; init; } = string.Empty;
        public string Beneficiary { get; init; } = string.Empty;
        public string SourceAccountNumber { get; init; } = string.Empty;
        public string DestinationAccountNumber { get; init; } = string.Empty;
        public string Description { get; init; } = string.Empty;
        public int SavingAccountId { get; init; }
        public TransactionStatus Status { get; init; } = TransactionStatus.Approved;
    }
}
