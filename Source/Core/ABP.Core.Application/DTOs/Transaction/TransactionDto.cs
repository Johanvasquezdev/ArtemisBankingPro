using ABP.Core.Domain.Enums;
using System.Text.Json.Serialization;

namespace ABP.Core.Application.DTOs.Transaction
{
    public class TransactionDto
    {
        public int Id { get; set; }
        public decimal Amount { get; set; }
        public DateTime TransactionDate { get; set; }
        
        [JsonIgnore]
        public TransactionType Type { get; set; }
        
        [JsonPropertyName("type")]
        public string TypeDisplay => Type switch
        {
            TransactionType.Debit => "Débito",
            TransactionType.Credit => "Crédito",
            TransactionType.Payment => "Pago",
            _ => Type.ToString()
        };
        
        public string Beneficiary { get; set; } = string.Empty;
        public string Origin { get; set; } = string.Empty;
        
        [JsonIgnore]
        public TransactionStatus Status { get; set; }
        
        [JsonPropertyName("status")]
        public string StatusDisplay => Status switch
        {
            TransactionStatus.Approved => "APROBADO",
            TransactionStatus.Declined => "RECHAZADO",
            _ => Status.ToString()
        };
        
        public int SavingAccountId { get; set; }
        public string SourceAccountNumber { get; set; } = string.Empty;
        public string DestinationAccountNumber { get; set; } = string.Empty;
        public DateTime CreatedAt { get; set; }
        public string Description { get; set; } = string.Empty; 
    }
}
