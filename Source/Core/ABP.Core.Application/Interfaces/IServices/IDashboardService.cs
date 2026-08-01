using ABP.Core.Application.DTOs.Dashboard;

namespace ABP.Core.Application.Interfaces.IServices
{
    public interface IDashboardService
    {
        Task<DashboardAdminDto> GetAdminDashboardAsync();
        Task<DashboardClientDto> GetClientDashboardAsync(string clientId);
        Task<DashboardCashierDto> GetCashierDashboardAsync(string cashierId);
    }
}
