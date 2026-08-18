using ABP.Core.Application.Interfaces.IServices;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    public sealed record ChangeCommerceStatusCommand(int Id, bool Status) : IRequest<bool>;

    public sealed class ChangeCommerceStatusCommandHandler(ICommerceService commerceService) : IRequestHandler<ChangeCommerceStatusCommand, bool>
    {
        private readonly ICommerceService _commerceService = commerceService;

        public async Task<bool> Handle(ChangeCommerceStatusCommand request, CancellationToken cancellationToken)
        {
            var existing = await _commerceService.GetByIdAsync(request.Id);
            if (existing == null) return false;

            await _commerceService.ChangeStatusAsync(request.Id, request.Status);
            return true;
        }
    }
}
