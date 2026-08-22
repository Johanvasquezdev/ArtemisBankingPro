using System;
using ABP.Core.Domain.Common;

namespace ABP.Core.Domain.Entities
{
    public class ScheduledPayment : AuditableBaseEntity
    {
        public int SavingsAccountId { get; set; }
        public SavingsAccount SavingsAccount { get; set; } = null!;
        public string ServiceName { get; set; } = string.Empty;
        public string ContractNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int ExecutionDay { get; set; }
        public bool IsActive { get; set; }
    }
}
