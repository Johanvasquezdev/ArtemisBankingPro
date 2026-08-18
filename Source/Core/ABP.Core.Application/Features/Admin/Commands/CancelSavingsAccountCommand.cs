using ABP.Core.Application.Interfaces.IServices;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    public sealed record CancelSavingsAccountCommand(string AccountNumber) : IRequest;

    public sealed class CancelSavingsAccountCommandHandler(ISavingsAccountService accountService) : IRequestHandler<CancelSavingsAccountCommand>
    {
        private readonly ISavingsAccountService _accountService = accountService;

        public async Task Handle(CancelSavingsAccountCommand request, CancellationToken cancellationToken)
        {
            await _accountService.CancelAsync(request.AccountNumber);
        }
    }
}
