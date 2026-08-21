using ABP.Core.Application.DTOs.Commerce;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    public sealed record CreateCommerceCommand(
        string Name, string? Description, string Logo,
        string Email, string PhoneNumber, string Rnc,
        string AdminId) : IRequest<CreateCommerceResult>;

    public sealed record CreateCommerceResult
    {
        public CommerceDto? Commerce { get; init; }
        public bool RncAlreadyExists { get; init; }
        public bool EmailAlreadyExists { get; init; }
    }

    public sealed class CreateCommerceCommandValidator : AbstractValidator<CreateCommerceCommand>
    {
        public CreateCommerceCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre del comercio es obligatorio.");
            RuleFor(x => x.Logo).NotNull().WithMessage("Logo is required.");
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("El correo es obligatorio y debe tener un formato valido.");
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("El telefono es obligatorio.");
            RuleFor(x => x.Rnc).NotEmpty().WithMessage("El RNC es obligatorio.");
        }
    }

    public sealed class CreateCommerceCommandHandler(ICommerceService commerceService)
        : IRequestHandler<CreateCommerceCommand, CreateCommerceResult>
    {
        public async Task<CreateCommerceResult> Handle(CreateCommerceCommand request, CancellationToken cancellationToken)
        {
            if (await commerceService.RncExistsAsync(request.Rnc))
                return new CreateCommerceResult { RncAlreadyExists = true };

            if (await commerceService.EmailExistsAsync(request.Email))
                return new CreateCommerceResult { EmailAlreadyExists = true };

            var dto = new CommerceDto
            {
                Name = request.Name,
                Description = request.Description!,
                Logo = request.Logo,
                Email = request.Email,
                PhoneNumber = request.PhoneNumber,
                Rnc = request.Rnc,
                CreatedByAdminId = request.AdminId
            };

            var created = await commerceService.AddAsync(dto);
            return new CreateCommerceResult { Commerce = created };
        }
    }
}