using System.ComponentModel.DataAnnotations;

namespace ABP.Core.Application.ViewModels.CreditCard
{
    public class EditCreditCardLimitViewModel
    {
        [Required]
        public int CardId { get; set; }

        public string CardNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El nuevo límite de crédito es requerido.")]
        [DataType(DataType.Currency)]
        [Range(1, double.MaxValue, ErrorMessage = "El límite de crédito debe ser mayor a cero.")]
        public decimal NewCreditLimit { get; set; }

        public decimal CurrentCreditLimit { get; set; }
        public decimal AmountOwed { get; set; }

        public bool HasError { get; set; }
        public string? Error { get; set; }
    }
}
