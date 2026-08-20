namespace ABP.Core.Application.DTOs.CreditCard
{
    public class AssignCreditCardDto
    {
        public string ClientId { get; set; } = string.Empty;
        public decimal CreditLimit { get; set; }
        public string AdminId { get; set; } = string.Empty;
    }
}
