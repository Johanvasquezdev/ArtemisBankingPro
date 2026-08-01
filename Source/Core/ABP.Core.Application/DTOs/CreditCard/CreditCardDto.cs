using ABP.Core.Domain.Enums;

namespace ABP.Core.Application.DTOs.CreditCard
{
    public class CreditCardDto
    {
        public int Id { get; set; }
        public string CardNumber { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public string ExpirationDate { get; set; } = string.Empty;
        public decimal AmountOwed { get; set; }
        public CardStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string ClientFullName { get; set; } = string.Empty;
    }
}
