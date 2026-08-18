using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Account.Commands;

public sealed record RegisterAccountCommand(
    string FirstName,
    string LastName,
    string Cedula,
    string Username,
    string Email,
    string Password,
    string Role,
    string AdminId,
    decimal InitialAmount,
    AccountEmailChannel EmailChannel = AccountEmailChannel.Web) : IRequest<bool>;

public sealed class RegisterAccountCommandValidator : AbstractValidator<RegisterAccountCommand>
{
    public RegisterAccountCommandValidator()
    {
        RuleFor(x => x.FirstName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.LastName).NotEmpty().MaximumLength(100);
        RuleFor(x => x.Cedula).NotEmpty();
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Email).NotEmpty().EmailAddress();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
        RuleFor(x => x.Role).NotEmpty();
        RuleFor(x => x.AdminId).NotEmpty();
        RuleFor(x => x.InitialAmount).GreaterThanOrEqualTo(0);
    }
}

public sealed class RegisterAccountCommandHandler(IUserService users)
    : IRequestHandler<RegisterAccountCommand, bool>
{
    public Task<bool> Handle(RegisterAccountCommand request, CancellationToken cancellationToken) =>
        users.RegisterAsync(request.FirstName, request.LastName, request.Cedula, request.Username,
            request.Email, request.Password, request.Role, request.AdminId, request.InitialAmount, request.EmailChannel);
}

public sealed record LogoutCommand : IRequest<Unit>;

public sealed class LogoutCommandHandler(IUserService users)
    : IRequestHandler<LogoutCommand, Unit>
{
    public async Task<Unit> Handle(LogoutCommand request, CancellationToken cancellationToken)
    {
        await users.LogoutAsync();
        return Unit.Value;
    }
}

public sealed record LoginCommand(string Username, string Password) : IRequest<AuthenticationResult>;

public sealed class LoginCommandValidator : AbstractValidator<LoginCommand>
{
    public LoginCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Password).NotEmpty();
    }
}

public sealed class LoginCommandHandler(IUserService users, IJwtService jwt)
    : IRequestHandler<LoginCommand, AuthenticationResult>
{
    public async Task<AuthenticationResult> Handle(LoginCommand request, CancellationToken cancellationToken)
    {
        var result = await users.AuthenticateAsync(request.Username, request.Password);
        if (!result.Success)
            return result;

        result.JwtToken = await jwt.GenerateTokenAsync(
            result.UserId,
            result.UserName,
            result.Email,
            [result.Role.ToString()],
            result.CommerceId);

        return result;
    }
}

public sealed record ActivateAccountCommand(string Token) : IRequest<bool>;

public sealed class ActivateAccountCommandValidator : AbstractValidator<ActivateAccountCommand>
{
    public ActivateAccountCommandValidator() => RuleFor(x => x.Token).NotEmpty();
}

public sealed class ActivateAccountCommandHandler(IUserService users)
    : IRequestHandler<ActivateAccountCommand, bool>
{
    public Task<bool> Handle(ActivateAccountCommand request, CancellationToken cancellationToken) =>
        users.ActivateAccountAsync(request.Token);
}

public sealed record GeneratePasswordResetTokenCommand(
    string Username,
    AccountEmailChannel EmailChannel = AccountEmailChannel.Web) : IRequest<bool>;

public sealed class GeneratePasswordResetTokenCommandValidator
    : AbstractValidator<GeneratePasswordResetTokenCommand>
{
    public GeneratePasswordResetTokenCommandValidator() => RuleFor(x => x.Username).NotEmpty();
}

public sealed class GeneratePasswordResetTokenCommandHandler(IUserService users)
    : IRequestHandler<GeneratePasswordResetTokenCommand, bool>
{
    public Task<bool> Handle(GeneratePasswordResetTokenCommand request, CancellationToken cancellationToken) =>
        users.GeneratePasswordResetTokenAsync(request.Username, request.EmailChannel);
}

public sealed record ResetPasswordCommand(
    string Username,
    string Token,
    string Password) : IRequest<bool>;

public sealed class ResetPasswordCommandValidator : AbstractValidator<ResetPasswordCommand>
{
    public ResetPasswordCommandValidator()
    {
        RuleFor(x => x.Username).NotEmpty();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public sealed class ResetPasswordCommandHandler(IUserService users)
    : IRequestHandler<ResetPasswordCommand, bool>
{
    public Task<bool> Handle(ResetPasswordCommand request, CancellationToken cancellationToken) =>
        users.ResetPasswordAsync(request.Username, request.Token, request.Password);
}

public sealed record ResetPasswordByUserIdCommand(
    string UserId,
    string Token,
    string Password) : IRequest<bool>;

public sealed class ResetPasswordByUserIdCommandValidator
    : AbstractValidator<ResetPasswordByUserIdCommand>
{
    public ResetPasswordByUserIdCommandValidator()
    {
        RuleFor(x => x.UserId).NotEmpty();
        RuleFor(x => x.Token).NotEmpty();
        RuleFor(x => x.Password).NotEmpty().MinimumLength(8);
    }
}

public sealed class ResetPasswordByUserIdCommandHandler(
    IUserService users,
    IUserReadOnlyService readOnlyUsers)
    : IRequestHandler<ResetPasswordByUserIdCommand, bool>
{
    public async Task<bool> Handle(ResetPasswordByUserIdCommand request, CancellationToken cancellationToken)
    {
        var user = await readOnlyUsers.GetByIdAsync(request.UserId);
        if (user is null || string.IsNullOrWhiteSpace(user.UserName))
            return false;

        return await users.ResetPasswordAsync(user.UserName, request.Token, request.Password);
    }
}
