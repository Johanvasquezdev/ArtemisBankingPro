using System.ComponentModel.DataAnnotations;

namespace ABP.Core.Application.ViewModels.Client
{
    public class PayBeneficiaryViewModel : TransactionFormViewModel
    {
        [Required(ErrorMessage = "Debe seleccionar un beneficiario.")]
        public int BeneficiaryId { get; set; }
    }
}
