using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    public sealed record UpdateCommerceCommand( int Id, string Name, string? Description, string Logo,
        string Email, string PhoneNumber, string Rnc) : IRequest<UpdateCommerceResult>;

    public sealed record UpdateCommerceResult
    {
        public bool NotFound { get; init; }
        public bool RncAlreadyExists { get; init; }
        public bool EmailAlreadyExists { get; init; }
        public bool Success { get; init; }
    }

    public sealed class UpdateCommerceCommandValidator : AbstractValidator<UpdateCommerceCommand>
    {
        public UpdateCommerceCommandValidator()
        {
            RuleFor(x => x.Name).NotEmpty().WithMessage("El nombre del comercio es obligatorio.");
            RuleFor(x => x.Logo).NotNull().WithMessage("Logo is required.");
            RuleFor(x => x.Email).NotEmpty().EmailAddress().WithMessage("El correo es obligatorio y debe tener un formato valido.");
            RuleFor(x => x.PhoneNumber).NotEmpty().WithMessage("El telefono es obligatorio.");
            RuleFor(x => x.Rnc).NotEmpty().WithMessage("El RNC es obligatorio.");
        }
    }

    public sealed class UpdateCommerceCommandHandler(ICommerceService commerceService) : IRequestHandler<UpdateCommerceCommand, UpdateCommerceResult>
    {
        public async Task<UpdateCommerceResult> Handle(UpdateCommerceCommand request, CancellationToken cancellationToken)
        {
            var existing = await commerceService.GetByIdAsync(request.Id);
            if (existing == null) return new UpdateCommerceResult { NotFound = true };

            if (await commerceService.RncExistsAsync(request.Rnc, request.Id))
                return new UpdateCommerceResult { RncAlreadyExists = true };

            if (await commerceService.EmailExistsAsync(request.Email, request.Id))
                return new UpdateCommerceResult { EmailAlreadyExists = true };

            existing.Name = request.Name;
            existing.Description = request.Description!;
            existing.Logo = request.Logo;
            existing.Email = request.Email;
            existing.PhoneNumber = request.PhoneNumber;
            existing.Rnc = request.Rnc;

            await commerceService.UpdateAsync(existing);
            return new UpdateCommerceResult { Success = true };
        }
    }
}