using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    public sealed record CancelSavingsAccountCommand(string AccountNumber) : IRequest<Unit>;

    public sealed class CancelSavingsAccountCommandValidator : AbstractValidator<CancelSavingsAccountCommand>
    {
        public CancelSavingsAccountCommandValidator() => RuleFor(x => x.AccountNumber).NotEmpty();
    }

    public sealed class CancelSavingsAccountCommandHandler(ISavingsAccountService accountService) : IRequestHandler<CancelSavingsAccountCommand, Unit>
    {
        private readonly ISavingsAccountService _accountService = accountService;

        public async Task<Unit> Handle(CancelSavingsAccountCommand request, CancellationToken cancellationToken)
        {
            await _accountService.CancelAsync(request.AccountNumber);
            return Unit.Value;
        }
    }
}
