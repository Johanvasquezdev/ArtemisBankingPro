using ABP.Core.Application.DTOs.User;
using System.ComponentModel.DataAnnotations;

namespace ABP.Core.Application.ViewModels.CreditCard
{
    public class AssignCreditCardViewModel
    {
        [Required(ErrorMessage = "El cliente es requerido.")]
        public string ClientId { get; set; } = string.Empty;

        [Required(ErrorMessage = "El límite de crédito es requerido.")]
        [DataType(DataType.Currency)]
        [Range(1, double.MaxValue, ErrorMessage = "El límite de crédito debe ser mayor a cero.")]
        public decimal CreditLimit { get; set; }
        public decimal AverageDebt { get; set; }
        public string? CurrentCedula { get; set; }
        public IEnumerable<UserDto> Clients { get; set; } = [];
        public bool HasError { get; set; }
        public string? Error { get; set; }
    }
}
