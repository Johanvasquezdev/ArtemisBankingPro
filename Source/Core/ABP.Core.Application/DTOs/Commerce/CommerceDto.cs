namespace ABP.Core.Application.DTOs.Commerce
{
    public class CommerceDto
    {
        public int Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public string Rnc { get; set; } = string.Empty;
        public string PhoneNumber { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Logo { get; set; } = string.Empty;
        public string? CreatedByAdminId { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public bool HasAssociatedUser { get; set; }
    }
}
