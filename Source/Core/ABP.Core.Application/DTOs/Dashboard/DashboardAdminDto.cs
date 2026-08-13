namespace ABP.Core.Application.DTOs.Dashboard
{
    public class DashboardAdminDto
    {
        public int TotalTransactions { get; set; }
        public int TodayPayments { get; set; }
        public int TotalPayments { get; set; }
        public int ActiveClients { get; set; }
        public int InactiveClients { get; set; }
        public int TotalProducts { get; set; }
        public int ActiveLoans { get; set; }
        public int ActiveCreditCards { get; set; }
        public int TotalSavingsAccounts { get; set; }
        public int TodayTransactions { get; set; }
        public decimal AverageDebt { get; set; }
    }
}
