using System.ComponentModel.DataAnnotations;

namespace ABP.Core.Application.ViewModels.Cashier
{
    public class CashierDepositViewModel
    {
        [Required(ErrorMessage ="El numero de cuenta es requerido.")]
        [StringLength(9, ErrorMessage = "El número de cuenta no es válido (9).", MinimumLength = 9)]
        public string AccountNumber { get; set; } = string.Empty;
        public bool HasError { get; set; }
        public string Error { get; set; } = string.Empty;
        public string AccountHolderName { get; set; } = string.Empty;
        public decimal Amount { get; set; }
    }
}
