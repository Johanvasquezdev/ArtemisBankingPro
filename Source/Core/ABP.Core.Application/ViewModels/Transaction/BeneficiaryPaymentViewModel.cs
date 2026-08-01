using System.ComponentModel.DataAnnotations;
using ABP.Core.Application.DTOs.Beneficiary;
using ABP.Core.Application.DTOs.SavingsAccount;

namespace ABP.Core.Application.ViewModels.Transaction
{
    public class BeneficiaryPaymentViewModel
    {
        [Required(ErrorMessage = "La cuenta de origen es requerida.")]
        public string SourceAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El beneficiario es requerido.")]
        public int BeneficiaryId { get; set; }

        [Required(ErrorMessage = "El monto es requerido.")]
        [DataType(DataType.Currency)]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero.")]
        public decimal Amount { get; set; }

        public IEnumerable<SavingsAccountDto> UserAccounts { get; set; } = [];
        public IEnumerable<BeneficiaryDto> Beneficiaries { get; set; } = [];

        public bool HasError { get; set; }
        public string? Error { get; set; }
    }
}
