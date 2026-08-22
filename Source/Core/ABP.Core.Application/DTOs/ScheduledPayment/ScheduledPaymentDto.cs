namespace ABP.Core.Application.DTOs.ScheduledPayment
{
    public class ScheduledPaymentDto
    {
        public int Id { get; set; }
        public int SavingsAccountId { get; set; }
        public string ServiceName { get; set; } = string.Empty;
        public string ContractNumber { get; set; } = string.Empty;
        public decimal Amount { get; set; }
        public int ExecutionDay { get; set; }
        public bool IsActive { get; set; }
    }
}