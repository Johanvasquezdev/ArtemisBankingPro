using System.ComponentModel.DataAnnotations;

namespace ABP.Core.Application.ViewModels.Client
{
    public class TransferOwnAccountsViewModel : TransactionFormViewModel
    {
        [Required(ErrorMessage = "La cuenta destino es requerida.")]
        [StringLength(9, ErrorMessage = "El número de cuenta destino no es válido.", MinimumLength = 9)]
        public string DestinationAccountNumber { get; set; } = string.Empty;
    }
}
