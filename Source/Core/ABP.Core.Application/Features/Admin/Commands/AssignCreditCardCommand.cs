using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Commands
{
    public sealed record AssignCreditCardCommand(string ClientId, decimal CreditLimit, string AdminId = "") : IRequest<CreditCardDto>
    {
        public AssignCreditCardCommand(AssignCreditCardDto card) : this(card.ClientId, card.CreditLimit, card.AdminId) { Card = card; }
        public AssignCreditCardDto Card { get; init; } = new() { ClientId = ClientId, CreditLimit = CreditLimit, AdminId = AdminId };
    }

    public sealed class AssignCreditCardCommandValidator : AbstractValidator<AssignCreditCardCommand>
    {
        public AssignCreditCardCommandValidator()
        {
            RuleFor(x => x.ClientId).NotEmpty().WithMessage("ClientId is required.");
            RuleFor(x => x.CreditLimit).GreaterThan(0).WithMessage("El límite de crédito debe ser mayor que cero.");
        }
    }

    public sealed class AssignCreditCardCommandHandler(ICreditCardService creditCardService)
        : IRequestHandler<AssignCreditCardCommand, CreditCardDto>
    {
        private readonly ICreditCardService _creditCardService = creditCardService;

        public async Task<CreditCardDto> Handle(AssignCreditCardCommand request, CancellationToken cancellationToken)
        {
            return await _creditCardService.AssignAsync(request.Card);
        }
    }
}
