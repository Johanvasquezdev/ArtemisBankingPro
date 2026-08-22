using System;

namespace ABP.Core.Application.DTOs.VirtualCard
{
    public class CreateVirtualCardDto
    {
        public int SavingsAccountId { get; set; }
        public decimal? LimitAmount { get; set; }
    }
}
