using System;
using ABP.Core.Domain.Common;

namespace ABP.Core.Domain.Entities
{
    public class SavingsGoal : AuditableBaseEntity
    {
        public int SavingsAccountId { get; set; }
        public SavingsAccount SavingsAccount { get; set; } = null!;
        public string Name { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public bool AutoRoundupEnabled { get; set; }
        public string ColorHex { get; set; } = string.Empty;
    }
}
