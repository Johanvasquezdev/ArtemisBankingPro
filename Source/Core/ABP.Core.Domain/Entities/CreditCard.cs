using ABP.Core.Domain.Enums;

namespace ABP.Core.Domain.Entities
{
    public class CreditCard
    {
        public int Id { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public string ExpirationDate { get; set; } = string.Empty;
        public decimal AmountOwed { get; set; }
        // 3-digit CVC code encrypted with SHA-256
        public string CVCHash { get; set; } = string.Empty;
        public CardStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public ICollection<CreditCardConsumption> Consumptions { get; set; } = [];

        // foreign key to the associated account
        public string ClientId { get; set; } = string.Empty;
        public string AssignedByAdminId { get; set; } = string.Empty;
        public decimal AvailableBalance => CreditLimit - AmountOwed;
    }
}