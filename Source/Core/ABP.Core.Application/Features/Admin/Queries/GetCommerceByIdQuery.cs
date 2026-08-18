using ABP.Core.Application.DTOs.Commerce;
using ABP.Core.Application.Interfaces.IServices;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Queries
{
    public sealed record GetCommerceByIdQuery(int Id) : IRequest<CommerceDto?>;

    public sealed class GetCommerceByIdQueryHandler(ICommerceService commerceService) : IRequestHandler<GetCommerceByIdQuery, CommerceDto?>
    {
        private readonly ICommerceService _commerceService = commerceService;

        public async Task<CommerceDto?> Handle(GetCommerceByIdQuery request, CancellationToken cancellationToken)
            => await _commerceService.GetByIdAsync(request.Id);
    }
}
