using ABP.Core.Domain.Enums;

namespace ABP.Core.Domain.Entities
{
    public class SavingsAccount
    {
        public int Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public AccountType Type { get; set; }
        public AccountStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool IsFrozen { get; set; } = false;
        public ICollection<Transaction> Transactions { get; set; } = [];

        // Foreign key to User
        public string UserId { get; set; } = string.Empty;
        public string? CreatedByAdminId { get; set; }
    }
}
