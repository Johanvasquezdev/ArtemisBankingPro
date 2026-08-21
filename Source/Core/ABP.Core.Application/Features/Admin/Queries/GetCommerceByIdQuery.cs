using ABP.Core.Application.DTOs.Commerce;
using ABP.Core.Application.Interfaces.IServices;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Queries
{
    public sealed record GetCommerceByIdQuery(int Id) : IRequest<GetCommerceByIdResult?>;
    public sealed record GetCommerceByIdResult(CommerceDto Commerce, AssociatedUserDto? AssociatedUser);
    public sealed class GetCommerceByIdQueryHandler(ICommerceService commerceService) : IRequestHandler<GetCommerceByIdQuery, GetCommerceByIdResult?>
    {
        public async Task<GetCommerceByIdResult?> Handle(GetCommerceByIdQuery request, CancellationToken cancellationToken)
        {
            var commerce = await commerceService.GetByIdAsync(request.Id);
            if (commerce == null) return null;

            var associatedUser = await commerceService.GetAssociatedUserAsync(request.Id);
            return new GetCommerceByIdResult(commerce, associatedUser);
        }
    }
}
