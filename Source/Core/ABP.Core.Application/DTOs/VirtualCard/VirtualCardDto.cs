using System;

namespace ABP.Core.Application.DTOs.VirtualCard
{
    public class VirtualCardDto
    {
        public int Id { get; set; }
        public int SavingsAccountId { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public string CVV { get; set; } = string.Empty;
        public DateTime ExpirationDate { get; set; }
        public decimal? LimitAmount { get; set; }
        public bool IsActive { get; set; }
        public bool IsFrozen { get; set; }
    }
}
