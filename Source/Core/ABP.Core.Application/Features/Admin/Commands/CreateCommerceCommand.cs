using ABP.Core.Application.DTOs.Commerce;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    public sealed record CreateCommerceCommand(
        string Name,
        string Description,
        string Logo,
        string Rnc,
        string Email) : IRequest<CommerceDto>
    {
        public CreateCommerceCommand(CommerceDto commerce)
            : this(commerce.Name, commerce.Description, commerce.Logo, commerce.Rnc, commerce.Email) { Commerce = commerce; }
        public CommerceDto Commerce { get; init; } = new() { Name = Name, Description = Description, Logo = Logo, Rnc = Rnc, Email = Email };
    }

    public sealed class CreateCommerceCommandValidator : AbstractValidator<CreateCommerceCommand>
    {
        public CreateCommerceCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("Name is required.");
            RuleFor(x => x.Description).NotEmpty().WithMessage("Description is required.");
            RuleFor(x => x.Logo).NotNull().WithMessage("Logo is required.");
            RuleFor(x => x.Commerce.Rnc).Matches("^[0-9]{9}$").WithMessage("El RNC debe contener exactamente 9 dígitos.");
            RuleFor(x => x.Commerce.Email).NotEmpty().EmailAddress();
        }
    }

    public sealed class CreateCommerceCommandHandler(ICommerceService commerceService) : IRequestHandler<CreateCommerceCommand, CommerceDto>
    {
        private readonly ICommerceService _commerceService = commerceService;

        public async Task<CommerceDto> Handle(CreateCommerceCommand request, CancellationToken cancellationToken)
        {
            var dto = request.Commerce;

            return await _commerceService.AddAsync(dto);
        }
    }
}
