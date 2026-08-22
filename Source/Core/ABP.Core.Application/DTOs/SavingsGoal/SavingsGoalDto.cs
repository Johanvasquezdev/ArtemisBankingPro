namespace ABP.Core.Application.DTOs.SavingsGoal
{
    public class SavingsGoalDto
    {
        public int Id { get; set; }
        public int SavingsAccountId { get; set; }
        public string Name { get; set; } = string.Empty;
        public decimal TargetAmount { get; set; }
        public decimal CurrentAmount { get; set; }
        public bool AutoRoundupEnabled { get; set; }
        public string ColorHex { get; set; } = string.Empty;
        public decimal Percentage => TargetAmount > 0 ? (CurrentAmount / TargetAmount) * 100 : 0;
    }
}