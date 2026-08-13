namespace ABP.Core.Application.DTOs.Account
{
    public class AssignSavingsAccountDto
    {
        public string ClientId { get; set; } = string.Empty;
        public string AdminId { get; set; } = string.Empty;
        public decimal InitialBalance { get; set; }
    }
}
