using ABP.Core.Domain.Enums;
using System.Text.Json.Serialization;

namespace ABP.Core.Application.DTOs.Loan
{
    public class LoanDto
    {
        public int Id { get; set; }
        public string LoanNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public int TotalInstallments { get; set; }
        public int PaidInstallments { get; set; }
        public decimal PendingAmount { get; set; }
        public int TermInMonths { get; set; }
        
        [JsonIgnore]
        public LoanStatus Status { get; set; }
        
        [JsonPropertyName("status")]
        public string StatusDisplay => Status switch
        {
            LoanStatus.Active => "Activo",
            LoanStatus.Completed => "Pagado",
            _ => Status.ToString()
        };
        
        public bool IsOnTime { get; set; }
        public string ClientFullName { get; set; } = string.Empty;
        public string ClientId { get; set; } = string.Empty;
    }
}
