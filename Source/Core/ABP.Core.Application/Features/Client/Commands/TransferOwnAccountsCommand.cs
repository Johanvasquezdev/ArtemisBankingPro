using ABP.Core.Application.DTOs.Common;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Client.Commands
{
    public record TransferOwnAccountsCommand(TransferOwnAccountsDto Dto) : IRequest<CommandResult>;

    public class TransferOwnAccountsCommandValidator : AbstractValidator<TransferOwnAccountsCommand>
    {
        public TransferOwnAccountsCommandValidator()
        {
            RuleFor(x => x.Dto.ClientId).NotEmpty().WithMessage("El usuario no es válido.");
            RuleFor(x => x.Dto.SourceAccountNumber)
                .NotEmpty().WithMessage("La cuenta de origen es requerida.")
                .Length(9).WithMessage("El número de cuenta de origen no es válido.");
            RuleFor(x => x.Dto.DestinationAccountNumber)
                .NotEmpty().WithMessage("La cuenta destino es requerida.")
                .Length(9).WithMessage("El número de cuenta destino no es válido.")
                .NotEqual(x => x.Dto.SourceAccountNumber)
                .WithMessage("La cuenta de origen y la cuenta de destino no pueden ser la misma.");
            RuleFor(x => x.Dto.Amount)
                .GreaterThan(0).WithMessage("El monto debe ser mayor a cero.");
        }
    }

    public class TransferOwnAccountsCommandHandler(IClientTransactionService transactionService)
        : IRequestHandler<TransferOwnAccountsCommand, CommandResult>
    {
        public Task<CommandResult> Handle(TransferOwnAccountsCommand request, CancellationToken cancellationToken)
            => transactionService.TransferOwnAccountsAsync(request.Dto);
    }
}
