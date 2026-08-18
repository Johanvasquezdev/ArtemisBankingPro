using ABP.Core.Application.DTOs.Commerce;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    public sealed record CreateCommerceCommand(string Name, string Description, string Logo) : IRequest<CommerceDto>;

    public sealed class CreateCommerceCommandValidator : AbstractValidator<CreateCommerceCommand>
    {
        public CreateCommerceCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");
            RuleFor(x => x.Logo).NotNull().WithMessage("Logo is required.");
        }
    }

    public sealed class CreateCommerceCommandHandler(ICommerceService commerceService) : IRequestHandler<CreateCommerceCommand, CommerceDto>
    {
        private readonly ICommerceService _commerceService = commerceService;

        public async Task<CommerceDto> Handle(CreateCommerceCommand request, CancellationToken cancellationToken)
        {
            var dto = new CommerceDto
            {
                Name = request.Name,
                Description = request.Description,
                Logo = request.Logo
            };

            await _commerceService.AddAsync(dto);
            return dto;
        }
    }
}
