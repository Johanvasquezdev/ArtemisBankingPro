using System.ComponentModel.DataAnnotations;

namespace ABP.Core.Application.ViewModels.Beneficiary
{
    public class BeneficiaryViewModel
    {
        [Required(ErrorMessage = "El número de cuenta es requerido.")]
        [StringLength(9, ErrorMessage = "El número de cuenta no es válido.", MinimumLength = 9)]
        public string AccountNumber { get; set; } = string.Empty;
        public string Alias { get; set; } = string.Empty;
        public string Id { get; set; } = string.Empty;
        public bool HasError { get; set; }
        public string? Error { get; set; }
    }
}
