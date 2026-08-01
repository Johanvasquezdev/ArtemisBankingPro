using ABP.Core.Domain.Enums;

namespace ABP.Core.Application.DTOs.CreditCardConsumption
{
    public class CreditCardConsumptionDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string CommerceName { get; set; } = string.Empty;
        public ConsumptionStatus Status { get; set; }
        public int CreditCardId { get; set; }
        public int? CommerceId { get; set; }
    }
}
