using ABP.Core.Domain.Enums;
using System.Text.Json.Serialization;

namespace ABP.Core.Application.DTOs.CreditCard
{
    public class CreditCardDto
    {
        public int Id { get; set; }
        
        [JsonIgnore]
        public string CardNumber { get; set; } = string.Empty;
        
        [JsonPropertyName("maskedCardNumber")]
        public string MaskedCardNumber => CardNumber.Length >= 4 ? $"****-****-****-{CardNumber[^4..]}" : CardNumber;
        
        [JsonPropertyName("lastFourDigits")]
        public string LastFourDigits => CardNumber.Length >= 4 ? CardNumber[^4..] : CardNumber;
        
        public decimal CreditLimit { get; set; }
        public string ExpirationDate { get; set; } = string.Empty;
        
        [JsonPropertyName("currentDebt")]
        public decimal AmountOwed { get; set; }
        
        [JsonIgnore]
        public CardStatus Status { get; set; }
        
        [JsonPropertyName("status")]
        public string StatusDisplay => Status switch
        {
            CardStatus.Active => "Activa",
            CardStatus.Inactive => "Inactiva",
            CardStatus.Blocked => "Bloqueada",
            CardStatus.Expired => "Expirada",
            CardStatus.Cancelled => "Cancelada",
            _ => Status.ToString()
        };
        
        public DateTime CreatedAt { get; set; }
        public string ClientId { get; set; } = string.Empty;
        public string ClientFullName { get; set; } = string.Empty;
        
        [JsonPropertyName("availableCredit")]
        public decimal AvailableBalance => CreditLimit - AmountOwed;
    }
}
