using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    public sealed record AssignSecondarySavingsAccountCommand(string CedulaClient, decimal InitialBalance, string AdminId)
        : IRequest<AssignSecondarySavingsAccountResult>
    {
        public AssignSecondarySavingsAccountCommand(AssignSavingsAccountDto account)
            : this(account.ClientId, account.InitialBalance, account.AdminId) { Account = account; }
        public AssignSavingsAccountDto? Account { get; init; }
    }

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
            RuleFor(x => x.AdminId).NotEmpty().WithMessage("AdminId is required.");
            RuleFor(x => x.InitialBalance).GreaterThanOrEqualTo(0).WithMessage("El balance inicial no puede ser negativo.");
        }
    }

    public sealed class AssignSecondarySavingsAccountCommandHandler : IRequestHandler<AssignSecondarySavingsAccountCommand, AssignSecondarySavingsAccountResult>
    {
        private readonly ISavingsAccountService _accountService;
        private readonly IUserReadOnlyService? _userReadOnlyService;

        public AssignSecondarySavingsAccountCommandHandler(ISavingsAccountService accountService, IUserReadOnlyService? userReadOnlyService = null)
        {
            _accountService = accountService;
            _userReadOnlyService = userReadOnlyService;
        }

        public async Task<AssignSecondarySavingsAccountResult> Handle(
            AssignSecondarySavingsAccountCommand request, CancellationToken cancellationToken)
        {
            if (request.Account is not null)
            {
                var primary = await _accountService.GetPrimaryAccountByClientIdAsync(request.Account.ClientId);
                if (primary is null)
                    throw new InvalidOperationException("El cliente debe tener una cuenta principal activa antes de poder asignarle una cuenta secundaria.");

                await _accountService.AssignSecondaryAsync(request.Account);
                return new AssignSecondarySavingsAccountResult { Success = true };
            }

            if (_userReadOnlyService is null)
                throw new InvalidOperationException("No se configuró el directorio de clientes.");

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

    // Adaptador de transporte para el endpoint que recibe Cédula. Mantiene la resolución
    // fuera del controlador y permite que Web/API compartan la misma regla de negocio.
    public sealed record AssignSecondarySavingsAccountByCedulaCommand(
        string CedulaClient, decimal InitialBalance, string AdminId) : IRequest<Unit>;

    public sealed class AssignSecondarySavingsAccountByCedulaCommandValidator
        : AbstractValidator<AssignSecondarySavingsAccountByCedulaCommand>
    {
        public AssignSecondarySavingsAccountByCedulaCommandValidator()
        {
            RuleFor(x => x.CedulaClient).NotEmpty();
            RuleFor(x => x.InitialBalance).GreaterThanOrEqualTo(0);
            RuleFor(x => x.AdminId).NotEmpty();
        }
    }

    public sealed class AssignSecondarySavingsAccountByCedulaCommandHandler(
        IUserReadOnlyService users, ISavingsAccountService accounts)
        : IRequestHandler<AssignSecondarySavingsAccountByCedulaCommand, Unit>
    {
        public async Task<Unit> Handle(AssignSecondarySavingsAccountByCedulaCommand request, CancellationToken cancellationToken)
        {
            var client = (await users.GetActiveClientsAsync(request.CedulaClient))
                .FirstOrDefault(x => x.Cedula == request.CedulaClient);
            if (client is null) throw new KeyNotFoundException("No se encontró ningún cliente activo con esta Cédula.");
            if (await accounts.GetPrimaryAccountByClientIdAsync(client.Id) is null)
                throw new InvalidOperationException("El cliente debe tener una cuenta principal activa antes de poder asignarle una cuenta secundaria.");

            await accounts.AssignSecondaryAsync(new AssignSavingsAccountDto
            {
                ClientId = client.Id, InitialBalance = request.InitialBalance, AdminId = request.AdminId
            });
            return Unit.Value;
        }
    }
}
