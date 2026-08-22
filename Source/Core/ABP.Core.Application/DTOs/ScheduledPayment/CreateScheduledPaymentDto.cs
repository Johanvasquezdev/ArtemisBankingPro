namespace ABP.Core.Application.DTOs.ScheduledPayment
{
    public class CreateScheduledPaymentDto
    {
        public int SavingsAccountId { get; set; }
        public string ServiceName { get; set; }
        public string ContractNumber { get; set; }
        public decimal Amount { get; set; }
        public int ExecutionDay { get; set; }
    }
}