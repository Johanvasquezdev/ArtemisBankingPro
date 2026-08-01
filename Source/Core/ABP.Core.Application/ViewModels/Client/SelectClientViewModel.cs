using ABP.Core.Application.DTOs.User;
using System.ComponentModel.DataAnnotations;

namespace ABP.Core.Application.ViewModels.Client
{
    public class SelectClientViewModel
    {
        [Required(ErrorMessage = "Average debt is required")]
        [Range(0.01, double.MaxValue, ErrorMessage = "Debt must be greater than 0")]
        public decimal? AverageDebt { get; set; }

        [Required(ErrorMessage = "At least one client must be selected")]
        public IEnumerable<UserDto>? Clients { get; set; }
        [Required(ErrorMessage = "Please select a client")]
        public string? SelectedClientId { get; set; }

        [Required(ErrorMessage = "ID number (Cedula) is required")]
        [StringLength(11, MinimumLength = 11, ErrorMessage = "Cedula must be 11 characters long")]
        [RegularExpression(@"^\d{11}$", ErrorMessage = "Invalid Cedula format. Use XXXXXXXXXXX")]
        public string? CurrentCedula { get; set; }
    }
}
