using System.ComponentModel.DataAnnotations;

namespace ABP.API.DTOs.Commerce
{
    public class UpdateCommerceRequest
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "RNC is required.")]
        [RegularExpression("^[0-9]{9}$", ErrorMessage = "El RNC debe contener exactamente 9 dígitos.")]
        public string Rnc { get; set; } = string.Empty;

        [Required(ErrorMessage = "Email is required.")]
        [EmailAddress(ErrorMessage = "El correo electrónico no es válido.")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Logo is required.")]
        public string Logo { get; set; } = string.Empty;
    }
}
