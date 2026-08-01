using ABP.Core.Domain.Enums;

namespace ABP.Core.Domain.Entities
{
    public class CreditCardConsumption
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        public string CommerceName { get; set; } = string.Empty;
        public ConsumptionStatus Status { get; set; }

        // Navigation properties
        public int CreditCardId { get; set; }
        public CreditCard CreditCard { get; set; } = null!;
        public int? CommerceId { get; set; }
        public Commerce? Commerce { get; set; }
    }
}
