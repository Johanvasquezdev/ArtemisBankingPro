using System.ComponentModel.DataAnnotations;

namespace ABP.Core.Application.ViewModels.Client
{
    public class PayLoanViewModel : TransactionFormViewModel
    {
        [Required(ErrorMessage = "El préstamo es requerido.")]
        public int LoanId { get; set; }
    }
}
