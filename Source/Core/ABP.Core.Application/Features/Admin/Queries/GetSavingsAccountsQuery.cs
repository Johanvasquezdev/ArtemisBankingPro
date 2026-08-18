using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.SavingsAccount;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Queries
{
    public sealed record GetSavingsAccountsQuery(
        int Page = 1, int PageSize = 20, string? Identification = null,
        string Status = "activa", string Type = "todas") : IRequest<PaginatedResult<SavingsAccountDto>>;

    public sealed class GetSavingsAccountsQueryValidator : AbstractValidator<GetSavingsAccountsQuery>
    {
        public GetSavingsAccountsQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0).WithMessage("Parámetros de paginación inválidos.");
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(20).WithMessage("Parámetros de paginación inválidos.");
            RuleFor(x => x.Status).Must(s => s is "activa" or "cancelada" or "todas")
                .WithMessage("El estado debe ser activa, cancelada o todas.");
            RuleFor(x => x.Type).Must(t => t is "principal" or "secundaria" or "todas")
                .WithMessage("El tipo debe ser principal, secundaria o todas.");
        }
    }

    public sealed class GetSavingsAccountsQueryHandler(ISavingsAccountService accountService)
        : IRequestHandler<GetSavingsAccountsQuery, PaginatedResult<SavingsAccountDto>>
    {
        private readonly ISavingsAccountService _accountService = accountService;

        public async Task<PaginatedResult<SavingsAccountDto>> Handle(GetSavingsAccountsQuery request, CancellationToken cancellationToken)
        {
            AccountStatus? parsedStatus = request.Status switch
            {
                "activa" => AccountStatus.Active,
                "cancelada" => AccountStatus.Closed,
                _ => null
            };

            AccountType? parsedType = request.Type switch
            {
                "principal" => AccountType.Primary,
                "secundaria" => AccountType.Secondary,
                _ => null
            };

            return await _accountService.GetAllPagedAsync(request.Page, request.PageSize, parsedStatus, parsedType, request.Identification);
        }
    }
}
