using System.ComponentModel.DataAnnotations;

namespace ABP.Core.Application.ViewModels.Loan
{
    public class EditLoanRateViewModel
    {
        [Required]
        public int LoanId { get; set; }

        public string LoanNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "La nueva tasa de interés es requerida.")]
        [Range(0.01, 100, ErrorMessage = "La tasa de interés debe estar entre {1}% y {2}%.")]
        public decimal NewAnnualInterestRate { get; set; }

        public decimal CurrentAnnualInterestRate { get; set; }

        public bool HasError { get; set; }
        public string? Error { get; set; }
    }
}
