using System;
using ABP.Core.Domain.Common;

namespace ABP.Core.Domain.Entities
{
    public class VirtualCard : AuditableBaseEntity
    {
        public int SavingsAccountId { get; set; }
        public SavingsAccount SavingsAccount { get; set; } = null!;
        public string CardNumber { get; set; } = string.Empty;
        public string CVV { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
        public decimal? LimitAmount { get; set; }
        public bool IsActive { get; set; }
        public bool IsFrozen { get; set; }
    }
}
