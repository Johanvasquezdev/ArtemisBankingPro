namespace ABP.Core.Application.DTOs.Dashboard
{
    using ABP.Core.Application.DTOs.Transaction;

    public class DashboardCashierDto
    {
        public int TodayTransactions { get; set; }
        public int TodayPayments { get; set; }
        public decimal TodayDeposits { get; set; }
        public decimal TodayWithdrawals { get; set; }
        public IReadOnlyList<TransactionDto> RecentTransactions { get; set; } = [];
    }
}
