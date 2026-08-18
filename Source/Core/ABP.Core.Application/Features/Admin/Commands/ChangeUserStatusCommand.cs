using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    public sealed record ChangeUserStatusCommand(string AdminId, string UserId, bool Status) : IRequest<ChangeUserStatusResult>;

    public sealed class ChangeUserStatusCommandValidator : AbstractValidator<ChangeUserStatusCommand>
    {
        public ChangeUserStatusCommandValidator()
        {
            RuleFor(x => x.AdminId).NotEmpty();
            RuleFor(x => x.UserId).NotEmpty();
        }
    }

    public sealed record ChangeUserStatusResult
    {
        public bool SelfModificationForbidden { get; init; }
        public bool UserNotFound { get; init; }
        public bool Success { get; init; }
    }

    public sealed class ChangeUserStatusCommandHandler(IUserService userService)
        : IRequestHandler<ChangeUserStatusCommand, ChangeUserStatusResult>
    {
        public async Task<ChangeUserStatusResult> Handle(ChangeUserStatusCommand request, CancellationToken cancellationToken)
        {
            if (request.AdminId == request.UserId)
                return new ChangeUserStatusResult { SelfModificationForbidden = true };

            var changed = await userService.ChangeStatusAsync(request.AdminId, request.UserId, request.Status);
            if (!changed)
                return new ChangeUserStatusResult { UserNotFound = true };

            return new ChangeUserStatusResult { Success = true };
        }
    }
}
