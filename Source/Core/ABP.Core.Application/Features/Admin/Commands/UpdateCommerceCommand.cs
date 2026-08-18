using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    // El bool de retorno indica si el comercio existia (false = el controller debe responder 404)
    public sealed record UpdateCommerceCommand(int Id, string Name, string Description, string Logo) : IRequest<bool>;

    public sealed class UpdateCommerceCommandValidator : AbstractValidator<UpdateCommerceCommand>
    {
        public UpdateCommerceCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");
            RuleFor(x => x.Logo).NotNull().WithMessage("Logo is required.");
        }
    }

    public sealed class UpdateCommerceCommandHandler(ICommerceService commerceService) : IRequestHandler<UpdateCommerceCommand, bool>
    {
        private readonly ICommerceService _commerceService = commerceService;

        public async Task<bool> Handle(UpdateCommerceCommand request, CancellationToken cancellationToken)
        {
            var existing = await _commerceService.GetByIdAsync(request.Id);
            if (existing == null) return false;

            existing.Name = request.Name;
            existing.Description = request.Description;
            existing.Logo = request.Logo;

            await _commerceService.UpdateAsync(existing);
            return true;
        }
    }
}
