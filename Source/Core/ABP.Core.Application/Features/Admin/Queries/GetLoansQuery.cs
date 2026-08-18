using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Queries
{
    public sealed record GetLoansQuery(int Page = 1, int PageSize = 20, string Status = "activos", string? Identification = null)
        : IRequest<PaginatedResult<LoanDto>>;

    public sealed class GetLoansQueryValidator : AbstractValidator<GetLoansQuery>
    {
        public GetLoansQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0).WithMessage("Parámetros de paginación inválidos.");
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(20).WithMessage("Parámetros de paginación inválidos.");
            RuleFor(x => x.Status).Must(s => s is "activos" or "completados" or "todos")
                .WithMessage("El estado debe ser activos, completados o todos.");
        }
    }

    public sealed class GetLoansQueryHandler(ILoanService loanService) : IRequestHandler<GetLoansQuery, PaginatedResult<LoanDto>>
    {
        private readonly ILoanService _loanService = loanService;

        public async Task<PaginatedResult<LoanDto>> Handle(GetLoansQuery request, CancellationToken cancellationToken)
        {
            LoanStatus? parsedStatus = request.Status switch
            {
                "activos" => LoanStatus.Active,
                "completados" => LoanStatus.Completed,
                _ => null
            };

            return await _loanService.GetAllPagedAsync(request.Page, request.PageSize, parsedStatus, request.Identification);
        }
    }
}
