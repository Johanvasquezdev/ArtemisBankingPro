using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    public sealed record UpdateUserCommand(
        string Id, string FirstName, string LastName, string Cedula, string Email, string UserName,
        string? Password, string? ConfirmPassword, decimal? AdditionalAmount) : IRequest<bool>;

    public sealed class UpdateUserCommandValidator : AbstractValidator<UpdateUserCommand>
    {
        public UpdateUserCommandValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
            RuleFor(x => x.Cedula).NotEmpty();
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.UserName).NotEmpty();
            RuleFor(x => x.ConfirmPassword)
                .NotEmpty().WithMessage("Debe confirmar la nueva contraseña.")
                .Equal(x => x.Password).WithMessage("Las contraseñas no coinciden.")
                .When(x => !string.IsNullOrWhiteSpace(x.Password));
            RuleFor(x => x.AdditionalAmount).GreaterThanOrEqualTo(0)
                .When(x => x.AdditionalAmount.HasValue)
                .WithMessage("El monto adicional no puede ser negativo.");
        }
    }

    public sealed class UpdateUserCommandHandler(IUserService userService) : IRequestHandler<UpdateUserCommand, bool>
    {
        public Task<bool> Handle(UpdateUserCommand request, CancellationToken cancellationToken)
        {
            var dto = new UpdateUserDto
            {
                Id = request.Id,
                FirstName = request.FirstName,
                LastName = request.LastName,
                Cedula = request.Cedula,
                Email = request.Email,
                Username = request.UserName,
                Password = request.Password,
                ConfirmPassword = request.ConfirmPassword,
                AdditionalAmount = request.AdditionalAmount
            };

            return userService.UpdateAsync(dto);
        }
    }
}
