using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Admin.Queries
{
    public sealed record GetCreditCardsQuery(int Page = 1, int PageSize = 20, string Status = "activa", string? Identification = null)
        : IRequest<PaginatedResult<CreditCardDto>>;

    public sealed class GetCreditCardsQueryValidator : AbstractValidator<GetCreditCardsQuery>
    {
        public GetCreditCardsQueryValidator()
        {
            RuleFor(x => x.Page).GreaterThan(0).WithMessage("Parámetros de paginación inválidos.");
            RuleFor(x => x.PageSize).GreaterThan(0).LessThanOrEqualTo(20).WithMessage("Parámetros de paginación inválidos.");
            RuleFor(x => x.Status).Must(s => s is "activa" or "cancelada" or "todas")
                .WithMessage("El estado debe ser activa, cancelada o todas.");
        }
    }

    public sealed class GetCreditCardsQueryHandler(ICreditCardService creditCardService)
        : IRequestHandler<GetCreditCardsQuery, PaginatedResult<CreditCardDto>>
    {
        private readonly ICreditCardService _creditCardService = creditCardService;

        public async Task<PaginatedResult<CreditCardDto>> Handle(GetCreditCardsQuery request, CancellationToken cancellationToken)
        {
            CardStatus? parsedStatus = request.Status switch
            {
                "activa" => CardStatus.Active,
                "cancelada" => CardStatus.Cancelled,
                _ => null
            };

            return await _creditCardService.GetAllPagedAsync(request.Page, request.PageSize, parsedStatus, request.Identification);
        }
    }
}
