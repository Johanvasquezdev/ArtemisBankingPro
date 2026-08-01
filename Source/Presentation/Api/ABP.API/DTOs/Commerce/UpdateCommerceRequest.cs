using System.ComponentModel.DataAnnotations;

namespace ABP.API.DTOs.Commerce
{
    public class UpdateCommerceRequest
    {
        [Required(ErrorMessage = "Name is required.")]
        public string Name { get; set; } = string.Empty;

        [Required(ErrorMessage = "Description is required.")]
        public string Description { get; set; } = string.Empty;

        [Required(ErrorMessage = "Logo is required.")]
        public string Logo { get; set; } = string.Empty;
    }
}
