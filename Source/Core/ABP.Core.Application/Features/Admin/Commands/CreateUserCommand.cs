using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.DTOs.Account;
using ABP.Core.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    public sealed record CreateUserCommand(
        string FirstName, string LastName, string Cedula, string UserName, string Email,
        string Password, string Role, string AdminId, decimal? InitialAmount,
        AccountEmailChannel EmailChannel = AccountEmailChannel.Api) : IRequest<CreateUserResult>;

    public sealed record CreateUserResult
    {
        public bool Success { get; init; }
        public bool CedulaAlreadyExists { get; init; }
        public bool UsernameOrEmailAlreadyExists { get; init; }
    }

    public sealed class CreateUserCommandValidator : AbstractValidator<CreateUserCommand>
    {
        public CreateUserCommandValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
            RuleFor(x => x.Cedula).NotEmpty();
            RuleFor(x => x.UserName).NotEmpty();
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password)
                .NotEmpty()
                .MinimumLength(8)
                .Matches(@"^(?=.*[a-z])(?=.*[A-Z])(?=.*\d)(?=.*[^a-zA-Z\d]).{8,}$")
                .WithMessage("La contraseña debe tener al menos 8 caracteres, una mayúscula, una minúscula, un número y un carácter especial.");
            RuleFor(x => x.Role)
                .Must(r => Enum.TryParse<UserRole>(r, true, out var parsed) && parsed != UserRole.Commerce)
                .WithMessage("El rol debe ser Administrador, Cajero o Cliente.");
            RuleFor(x => x.InitialAmount).GreaterThanOrEqualTo(0)
                .When(x => x.InitialAmount.HasValue)
                .WithMessage("El monto inicial no puede ser negativo.");
        }
    }

    public sealed class CreateUserCommandHandler : IRequestHandler<CreateUserCommand, CreateUserResult>
    {
        private readonly IUserService _userService;
        private readonly IUserReadOnlyService? _userReadOnlyService;

        public CreateUserCommandHandler(IUserService userService, IUserReadOnlyService? userReadOnlyService = null)
        {
            _userService = userService;
            _userReadOnlyService = userReadOnlyService;
        }

        public async Task<CreateUserResult> Handle(CreateUserCommand request, CancellationToken cancellationToken)
        {
            if (_userReadOnlyService is not null && await _userReadOnlyService.ExistsByCedulaAsync(request.Cedula))
                return new CreateUserResult { CedulaAlreadyExists = true };

            var created = await _userService.RegisterAsync(
                request.FirstName, request.LastName, request.Cedula, request.UserName,
                request.Email, request.Password, request.Role, request.AdminId, request.InitialAmount ?? 0,
                request.EmailChannel);

            if (!created)
                return new CreateUserResult { UsernameOrEmailAlreadyExists = true };

            return new CreateUserResult { Success = true };
        }
    }
}
