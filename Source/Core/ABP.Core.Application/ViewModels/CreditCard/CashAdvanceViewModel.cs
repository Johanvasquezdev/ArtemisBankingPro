using System.ComponentModel.DataAnnotations;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.SavingsAccount;

namespace ABP.Core.Application.ViewModels.CreditCard
{
    public class CashAdvanceViewModel
    {
        [Required(ErrorMessage = "La tarjeta de crédito es requerida.")]
        public int CreditCardId { get; set; }

        [Required(ErrorMessage = "La cuenta de ahorro es requerida.")]
        public int SavingsAccountId { get; set; }

        [Required(ErrorMessage = "El monto es requerido.")]
        [DataType(DataType.Currency)]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero.")]
        public decimal Amount { get; set; }

        public IEnumerable<CreditCardDto> UserCreditCards { get; set; } = [];
        public IEnumerable<SavingsAccountDto> UserAccounts { get; set; } = [];

        public bool HasError { get; set; }
        public string? Error { get; set; }
    }
}
