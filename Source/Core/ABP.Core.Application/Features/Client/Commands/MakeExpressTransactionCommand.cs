using ABP.Core.Application.DTOs.Common;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Client.Commands
{
    public record MakeExpressTransactionCommand(MakeExpressTransactionDto Dto) : IRequest<CommandResult>;

    public class MakeExpressTransactionCommandValidator : AbstractValidator<MakeExpressTransactionCommand>
    {
        public MakeExpressTransactionCommandValidator()
        {
            RuleFor(x => x.Dto.ClientId).NotEmpty().WithMessage("El usuario no es válido.");
            RuleFor(x => x.Dto.SourceAccountNumber)
                .NotEmpty().WithMessage("La cuenta de origen es requerida.")
                .Length(9).WithMessage("El número de cuenta de origen no es válido.");
            RuleFor(x => x.Dto.DestinationAccountNumber)
                .NotEmpty().WithMessage("La cuenta destino es requerida.")
                .Length(9).WithMessage("El número de cuenta destino no es válido.")
                .NotEqual(x => x.Dto.SourceAccountNumber)
                .WithMessage("La cuenta destino no puede ser la misma cuenta de origen.");
            RuleFor(x => x.Dto.Amount)
                .GreaterThan(0).WithMessage("El monto debe ser mayor a cero.");
        }
    }

    public class MakeExpressTransactionCommandHandler(IClientTransactionService transactionService)
        : IRequestHandler<MakeExpressTransactionCommand, CommandResult>
    {
        public Task<CommandResult> Handle(MakeExpressTransactionCommand request, CancellationToken cancellationToken)
            => transactionService.MakeExpressTransactionAsync(request.Dto);
    }
}
