namespace ABP.Core.Application.ViewModels.Dashboard
{
    public class AdminDashboardViewModel
    {
        public int TotalTransactions { get; set; }
        public int TodayTransactions { get; set; }
        public int TotalProducts { get; set; }
        public int ActiveLoans { get; set; }
        public int ActiveCreditCards { get; set; }
        public int TotalSavingsAccounts { get; set; }
        public int TotalInactiveTransactions { get; set; }
        public decimal TotalDailyPayments { get; set; }
        public int TotalAssignedProducts { get; set; }
        public int TotalActiveClients { get; set; }
        public int TotalInactiveClients { get; set; }
        public decimal AverageDebt { get; set; }
    }
}
