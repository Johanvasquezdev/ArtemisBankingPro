namespace ABP.Core.Application.DTOs.SavingsGoal
{
    public class CreateSavingsGoalDto
    {
        public int SavingsAccountId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public bool AutoRoundupEnabled { get; set; }
        public string ColorHex { get; set; } = string.Empty;
    }
}