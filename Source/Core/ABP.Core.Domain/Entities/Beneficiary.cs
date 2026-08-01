namespace ABP.Core.Domain.Entities
{
    public class Beneficiary
    {
        public int Id { get; set; }
        public string AccountNumber { get; set; } = string.Empty;
        public string FirstName { get; set; } = string.Empty;
        public string LastName { get; set; } = string.Empty;
        // foreign key to the owner of the beneficiary
        public string OwnerId { get; set; } = string.Empty;
    }
}
