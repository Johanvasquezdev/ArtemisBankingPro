namespace ABP.Core.Application.DTOs.Dashboard
{
    public class DashboardCashierDto
    {
        public int TodayTransactions { get; set; }
        public int TodayPayments { get; set; }
        public int TodayDeposits { get; set; }
        public int TodayWithdrawals { get; set; }
    }
}
