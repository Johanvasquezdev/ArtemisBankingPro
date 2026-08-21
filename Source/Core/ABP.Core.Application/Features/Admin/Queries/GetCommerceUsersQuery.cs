using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Queries
{
    public sealed record GetCommerceUsersQuery(int Page = 1, int PageSize = 20) : IRequest<PaginatedResult<UserDto>>;

    public sealed class GetCommerceUsersQueryValidator : AbstractValidator<GetCommerceUsersQuery>
    {
        public GetCommerceUsersQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0).WithMessage("Parámetros de paginación inválidos.");
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(20).WithMessage("Parámetros de paginación inválidos.");
        }
    }

    public sealed class GetCommerceUsersQueryHandler(IUserReadOnlyService userReadOnlyService)
        : IRequestHandler<GetCommerceUsersQuery, PaginatedResult<UserDto>>
    {
        public Task<PaginatedResult<UserDto>> Handle(GetCommerceUsersQuery request, CancellationToken cancellationToken)
            => userReadOnlyService.GetCommerceUsersAsync(request.Page, request.PageSize);
    }
}
