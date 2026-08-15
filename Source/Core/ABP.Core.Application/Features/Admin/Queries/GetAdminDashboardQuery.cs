using ABP.Core.Application.DTOs.Dashboard;
using ABP.Core.Application.Interfaces.IServices;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Queries
{
    public sealed record GetAdminDashboardQuery : IRequest<DashboardAdminDto>;

    public sealed class GetAdminDashboardQueryHandler(IDashboardService dashboardService)
        : IRequestHandler<GetAdminDashboardQuery, DashboardAdminDto>
    {
        public Task<DashboardAdminDto> Handle(GetAdminDashboardQuery request, CancellationToken cancellationToken)
            => dashboardService.GetAdminDashboardAsync();
    }
}
