using System.ComponentModel.DataAnnotations;

namespace ABP.API.DTOs.Commerce
{
    public class UpdateCommerceRequest
    {
        [Required(ErrorMessage = "Name es requerido.")]
        public string Name { get; set; } = string.Empty;

        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "RNC es requerido.")]
        [RegularExpression("^[0-9]{9}$", ErrorMessage = "El RNC debe contener exactamente 9 digitos.")]
        public string Rnc { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email es requerido.")]
        [EmailAddress(ErrorMessage = "El correo no es valido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "PhoneNumber es requerido.")]
        public string PhoneNumber { get; set; } = string.Empty;

        [Required(ErrorMessage = "Logo es requerido.")]
        public string Logo { get; set; } = string.Empty;
    }
}
