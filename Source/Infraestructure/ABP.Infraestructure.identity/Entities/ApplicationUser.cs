using ABP.Core.Domain.Enums;
using Microsoft.AspNetCore.Identity;

namespace ABP.Infraestructure.identity.Entities
{
    public class ApplicationUser : IdentityUser
    {
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string Cedula { get; set; } = string.Empty;
        public UserRole Role { get; set; }
        public string? ActivationToken { get; set; }
        public bool IsActive { get; set; } = false;
        public int? CommerceId { get; set; }
    }
}
