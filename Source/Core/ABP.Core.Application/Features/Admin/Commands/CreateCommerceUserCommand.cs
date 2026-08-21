using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.DTOs.Account;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    public sealed record CreateCommerceUserCommand(
        string FirstName, string LastName, string Cedula, string UserName, string Email,
        string Password, int CommerceId,
        AccountEmailChannel EmailChannel = AccountEmailChannel.Api) : IRequest<bool>;

    public sealed class CreateCommerceUserCommandValidator : AbstractValidator<CreateCommerceUserCommand>
    {
        public CreateCommerceUserCommandValidator()
        {
            RuleFor(x => x.FirstName).NotEmpty();
            RuleFor(x => x.LastName).NotEmpty();
            RuleFor(x => x.Cedula).NotEmpty();
            RuleFor(x => x.UserName).NotEmpty();
            RuleFor(x => x.Email).NotEmpty().EmailAddress();
            RuleFor(x => x.Password).NotEmpty();
            RuleFor(x => x.CommerceId).GreaterThan(0);
        }
    }

    public sealed class CreateCommerceUserCommandHandler(IUserService userService) : IRequestHandler<CreateCommerceUserCommand, bool>
    {
        public Task<bool> Handle(CreateCommerceUserCommand request, CancellationToken cancellationToken)
            => userService.RegisterCommerceUserAsync(
                request.FirstName, request.LastName, request.Cedula, request.UserName, request.Email,
                request.Password, request.CommerceId, request.EmailChannel);
    }
}
