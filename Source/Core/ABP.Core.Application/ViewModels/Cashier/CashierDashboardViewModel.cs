namespace ABP.Core.Application.ViewModels.Cashier
{
    using ABP.Core.Application.DTOs.Transaction;

    public class CashierDashboardViewModel
    {
        public int TodayTransactions { get; set; }
        public int TodayPayments { get; set; }
        public decimal TodayDeposits { get; set; }
        public decimal TodayWithdrawals { get; set; }
        public IReadOnlyList<TransactionDto> RecentTransactions { get; set; } = [];
    }
}
