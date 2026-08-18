using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.Interfaces.IServices;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Queries
{
    public sealed record GetLoanByIdQuery(int Id) : IRequest<LoanDto?>;

    public sealed class GetLoanByIdQueryHandler(ILoanService loanService) : IRequestHandler<GetLoanByIdQuery, LoanDto?>
    {
        private readonly ILoanService _loanService = loanService;

        public async Task<LoanDto?> Handle(GetLoanByIdQuery request, CancellationToken cancellationToken)
            => await _loanService.GetByIdAsync(request.Id);
    }
}
