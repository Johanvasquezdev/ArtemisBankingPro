using System.ComponentModel.DataAnnotations;
using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.DTOs.SavingsAccount;

namespace ABP.Core.Application.ViewModels.Transaction
{
    public class LoanPaymentViewModel
    {
        [Required(ErrorMessage = "La cuenta de origen es requerida.")]
        public string SourceAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El préstamo es requerido.")]
        public string LoanNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto es requerido.")]
        [DataType(DataType.Currency)]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero.")]
        public decimal Amount { get; set; }

        public IEnumerable<SavingsAccountDto> UserAccounts { get; set; } = [];
        public IEnumerable<LoanDto> UserLoans { get; set; } = [];

        public bool HasError { get; set; }
        public string? Error { get; set; }
    }
}
