namespace ABP.Core.Application.DTOs.Beneficiary
{
    public class BeneficiaryDto
    {
        public int Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        public string OwnerId { get; set; } = string.Empty;
    }
}
