using ABP.Core.Domain.Enums;
using System.Text.Json.Serialization;

namespace ABP.Core.Application.DTOs.SavingsAccount
{
    public class SavingsAccountDto
    {
        public int Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public decimal Balance { get; set; }
        
        [JsonIgnore]
        public AccountType Type { get; set; }
        
        [JsonPropertyName("type")]
        public string TypeDisplay => Type switch
        {
            AccountType.Main => "Principal",
            AccountType.Secondary => "Secundaria",
            _ => Type.ToString()
        };
        
        [JsonIgnore]
        public AccountStatus Status { get; set; }
        
        [JsonPropertyName("status")]
        public string StatusDisplay => Status switch
        {
            AccountStatus.Active => "Activa",
            AccountStatus.Inactive => "Inactiva",
            _ => Status.ToString()
        };
        
        public DateTime CreatedAt { get; set; }
        public string UserId { get; set; } = string.Empty;
        public bool IsActive { get; set; }
        public string OwnerFullName { get; set; } = string.Empty;
    }
}
