using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Queries
{
    public sealed record GetUsersQuery(int Page = 1, int PageSize = 20, string? Role = null) : IRequest<PaginatedResult<UserDto>>;

    public sealed class GetUsersQueryValidator : AbstractValidator<GetUsersQuery>
    {
        public GetUsersQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0).WithMessage("Parámetros de paginación inválidos.");
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(20).WithMessage("Parámetros de paginación inválidos.");
            RuleFor(x => x.Role)
                .Must(r => string.IsNullOrWhiteSpace(r) || (Enum.TryParse<UserRole>(r, true, out var parsed) && parsed != UserRole.Commerce))
                .WithMessage("Filtro de rol inválido.");
        }
    }

    public sealed class GetUsersQueryHandler(IUserReadOnlyService userReadOnlyService)
        : IRequestHandler<GetUsersQuery, PaginatedResult<UserDto>>
    {
        public async Task<PaginatedResult<UserDto>> Handle(GetUsersQuery request, CancellationToken cancellationToken)
        {
            UserRole? parsedRole = null;
            if (!string.IsNullOrWhiteSpace(request.Role) && Enum.TryParse<UserRole>(request.Role, true, out var role))
                parsedRole = role;

            return await userReadOnlyService.GetAllAsync(request.Page, request.PageSize, parsedRole);
        }
    }
}
