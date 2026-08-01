using ABP.Core.Domain.Enums;

namespace ABP.Core.Application.DTOs.User
{
    public class UpdateUserDto
    {
        public string Id { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? Password { get; set; }
        public string? ConfirmPassword { get; set; }
        public decimal? AdditionalAmount { get; set; }
    }
}
