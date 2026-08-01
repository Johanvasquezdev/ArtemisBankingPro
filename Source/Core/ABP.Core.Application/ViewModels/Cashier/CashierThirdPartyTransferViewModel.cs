using System.ComponentModel.DataAnnotations;

namespace ABP.Core.Application.ViewModels.Cashier
{
    public class CashierThirdPartyTransferViewModel
    {
        public string SourceAccountNumber { get; set; } = string.Empty;
        public bool HasError { get; set; }
        public string Error { get; set; } = string.Empty;
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "El numero de cuenta es requerido.")]
        [StringLength(9, ErrorMessage = "El número de cuenta no es válido (9).", MinimumLength = 9)]
        public string DestinationAccountNumber { get; set; } = string.Empty;
        public string DestinationHolderName { get; set; } = string.Empty;
    }
}
