using System.ComponentModel.DataAnnotations;

namespace ABP.API.DTOs.Commerce
{
    public class ChangeCommerceStatusRequest
    {
        [Required(ErrorMessage = "Status is required.")]
        public bool Status { get; set; }
    }
}
