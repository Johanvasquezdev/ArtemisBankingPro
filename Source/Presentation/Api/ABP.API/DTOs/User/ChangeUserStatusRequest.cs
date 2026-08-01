using System.ComponentModel.DataAnnotations;

namespace ABP.API.DTOs.User
{
    public class ChangeUserStatusRequest
    {
        [Required(ErrorMessage = "Status is required.")]
        public bool Status { get; set; }
    }
}
