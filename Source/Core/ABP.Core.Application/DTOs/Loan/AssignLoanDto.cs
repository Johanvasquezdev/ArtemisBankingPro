namespace ABP.Core.Application.DTOs.Loan
{
    public class AssignLoanDto
    {
        public string ClientId { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public decimal AnnualInterestRate { get; set; }
        public int TermInMonths { get; set; }
        public string AdminId { get; set; } = string.Empty;
    }
}
