using ABP.Core.Domain.Enums;

namespace ABP.Core.Application.DTOs.SavingsAccount
{
    public class SavingsAccountDto
    {
        public int Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        public AccountType Type { get; set; }
        public AccountStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string UserId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string OwnerFullName { get; set; } = string.Empty;
    }
}
