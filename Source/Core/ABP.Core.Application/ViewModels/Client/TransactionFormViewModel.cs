using System.ComponentModel.DataAnnotations;

namespace ABP.Core.Application.ViewModels.Client
{
    public abstract class TransactionFormViewModel
    {
        [Required(ErrorMessage = "La cuenta de origen es requerida.")]
        public string SourceAccountNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto es requerido.")]
        [DataType(DataType.Currency)]
        [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero.")]
        public decimal Amount { get; set; }

        public TransactionOptionsViewModel Options { get; set; } = new();
        public bool HasError { get; set; }
        public string? Error { get; set; }
        public bool EmailNotificationFailed { get; set; }
    }
}
