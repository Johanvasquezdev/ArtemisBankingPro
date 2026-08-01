using System.ComponentModel.DataAnnotations;

namespace ABP.Core.Application.ViewModels.Loan
{
    public class AssignLoanViewModel
    {
        [Required(ErrorMessage = "El cliente es requerido.")]
        public string ClientId { get; set; } = string.Empty;

        public string ClientName { get; set; } = string.Empty;

        [Required(ErrorMessage = "El monto del préstamo es requerido.")]
        [DataType(DataType.Currency)]
        [Range(1, double.MaxValue, ErrorMessage = "El monto debe ser mayor a cero.")]
        public decimal Amount { get; set; }

        [Required(ErrorMessage = "La tasa de interés anual es requerida.")]
        [Range(0.01, 100, ErrorMessage = "La tasa de interés debe estar entre {1}% y {2}%.")]
        public decimal AnnualInterestRate { get; set; }

        [Required(ErrorMessage = "El plazo en meses es requerido.")]
        [Range(1, 360, ErrorMessage = "El plazo debe estar entre {1} y {2} meses.")]
        public int TermInMonths { get; set; }

        public bool IsHighRisk { get; set; }

        public bool RiskConfirmed { get; set; }

        public string? RiskMessage { get; set; }
        public bool HasError { get; set; }
        public string? Error { get; set; }
        public decimal AverageDebt { get; set; }
        public decimal CurrentDebt { get; set; }
    }
}
