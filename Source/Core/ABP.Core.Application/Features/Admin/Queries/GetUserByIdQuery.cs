using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Interfaces.IServices;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Queries
{
    public sealed record GetUserByIdQuery(string Id) : IRequest<UserDto?>;

    public sealed class GetUserByIdQueryHandler(IUserReadOnlyService userReadOnlyService)
        : IRequestHandler<GetUserByIdQuery, UserDto?>
    {
        public Task<UserDto?> Handle(GetUserByIdQuery request, CancellationToken cancellationToken)
            => userReadOnlyService.GetByIdAsync(request.Id);
    }
}
