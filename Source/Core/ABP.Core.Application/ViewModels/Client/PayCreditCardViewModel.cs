using System.ComponentModel.DataAnnotations;

namespace ABP.Core.Application.ViewModels.Client
{
    public class PayCreditCardViewModel : TransactionFormViewModel
    {
        [Required(ErrorMessage = "La tarjeta de crédito es requerida.")]
        public int CreditCardId { get; set; }
    }
}
