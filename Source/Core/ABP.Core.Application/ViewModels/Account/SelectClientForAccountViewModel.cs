using ABP.Core.Application.DTOs.User;

namespace ABP.Core.Application.ViewModels.Account
{
    public class SelectClientForAccountViewModel
    {
        public string? SelectedClientId { get; set; }

        public IEnumerable<UserDto> Clients { get; set; } = [];
        public string? CurrentCedula { get; set; }
    }
}
