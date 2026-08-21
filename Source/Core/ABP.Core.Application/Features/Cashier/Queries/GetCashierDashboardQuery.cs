using ABP.Core.Application.DTOs.Dashboard;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Interfaces.IServices;
using MediatR;

namespace ABP.Core.Application.Features.Cashier.Queries
{
    public sealed record GetCashierDashboardQuery(string CashierId) : IRequest<DashboardCashierDto>;

    public sealed class GetCashierDashboardQueryHandler(IDashboardService dashboardService)
        : IRequestHandler<GetCashierDashboardQuery, DashboardCashierDto>
    {
        public Task<DashboardCashierDto> Handle(GetCashierDashboardQuery request, CancellationToken cancellationToken)
            => dashboardService.GetCashierDashboardAsync(request.CashierId);
    }

    public sealed record GetCashierHistoryQuery(int Take = 100) : IRequest<IEnumerable<TransactionDto>>;

    public sealed class GetCashierHistoryQueryHandler(ITransactionQueryService transactionService)
        : IRequestHandler<GetCashierHistoryQuery, IEnumerable<TransactionDto>>
    {
        public Task<IEnumerable<TransactionDto>> Handle(GetCashierHistoryQuery request, CancellationToken cancellationToken)
            => transactionService.GetHistoryAsync(request.Take);
    }
}
