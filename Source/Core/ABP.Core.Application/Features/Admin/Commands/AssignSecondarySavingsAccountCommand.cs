using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    public sealed record AssignSecondarySavingsAccountCommand(string CedulaClient, decimal InitialBalance, string AdminId)
        : IRequest<AssignSecondarySavingsAccountResult>;

    public sealed record AssignSecondarySavingsAccountResult
    {
        public bool ClientNotFound { get; init; }
        public bool ClientHasNoPrimaryAccount { get; init; }
        public bool Success { get; init; }
    }

    public sealed class AssignSecondarySavingsAccountCommandValidator : AbstractValidator<AssignSecondarySavingsAccountCommand>
    {
        public AssignSecondarySavingsAccountCommandValidator()
        {
            RuleFor(x => x.CedulaClient).NotEmpty().WithMessage("CedulaClient is required.");
            RuleFor(x => x.InitialBalance).GreaterThanOrEqualTo(0).WithMessage("El balance inicial no puede ser negativo.");
        }
    }

    public sealed class AssignSecondarySavingsAccountCommandHandler(
        ISavingsAccountService accountService, IUserReadOnlyService userReadOnlyService)
        : IRequestHandler<AssignSecondarySavingsAccountCommand, AssignSecondarySavingsAccountResult>
    {
        private readonly ISavingsAccountService _accountService = accountService;
        private readonly IUserReadOnlyService _userReadOnlyService = userReadOnlyService;

        public async Task<AssignSecondarySavingsAccountResult> Handle(
            AssignSecondarySavingsAccountCommand request, CancellationToken cancellationToken)
        {
            var matches = await _userReadOnlyService.GetActiveClientsAsync(request.CedulaClient);
            var client = matches.FirstOrDefault(c => c.Cedula == request.CedulaClient);

            if (client == null)
                return new AssignSecondarySavingsAccountResult { ClientNotFound = true };

            var primaryAccount = await _accountService.GetPrimaryAccountByClientIdAsync(client.Id);
            if (primaryAccount == null)
                return new AssignSecondarySavingsAccountResult { ClientHasNoPrimaryAccount = true };

            var dto = new AssignSavingsAccountDto
            {
                ClientId = client.Id,
                AdminId = request.AdminId,
                InitialBalance = request.InitialBalance
            };

            await _accountService.AssignSecondaryAsync(dto);
            return new AssignSecondarySavingsAccountResult { Success = true };
        }
    }
}
