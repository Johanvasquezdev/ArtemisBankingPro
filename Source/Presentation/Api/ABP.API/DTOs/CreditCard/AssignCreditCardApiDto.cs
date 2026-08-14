using System.ComponentModel.DataAnnotations;

namespace ABP.API.DTOs.CreditCard
{
    public class AssignCreditCardApiDto
    {
        [Required(ErrorMessage = "ClientId es requerido.")]
        public string ClientId { get; set; } = string.Empty;

        [Required(ErrorMessage = "El limite de credito es requerido.")]
        [Range(0.01, double.MaxValue, ErrorMessage = "el limite de credito debe ser mayor que 0.01.")]
        public decimal CreditLimit { get; set; }
    }
}
