using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    public sealed record ChangeCommerceStatusCommand(int Id, bool Status) : IRequest<bool>;

    public sealed class ChangeCommerceStatusCommandValidator : AbstractValidator<ChangeCommerceStatusCommand>
    {
        public ChangeCommerceStatusCommandValidator() => RuleFor(x => x.Id).GreaterThan(0);
    }

    public sealed class ChangeCommerceStatusCommandHandler(ICommerceService commerceService, IUserService userService)
        : IRequestHandler<ChangeCommerceStatusCommand, bool>
    {
        public async Task<bool> Handle(ChangeCommerceStatusCommand request, CancellationToken cancellationToken)
        {
            var existing = await commerceService.GetByIdAsync(request.Id);
            if (existing == null) return false;

            await commerceService.ChangeStatusAsync(request.Id, request.Status);
            if (!request.Status)
            {
                await userService.DeactivateUsersByCommerceIdAsync(request.Id);
            }

            return true;
        }
    }
}
