using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.Payment;
using ABP.Core.Application.Interfaces.IServices;
using FluentValidation;
using MediatR;

namespace ABP.Core.Application.Features.Commerce.Queries;

public sealed record GetCommerceTransactionsQuery(int CommerceId, int Page = 1, int PageSize = 20)
    : IRequest<PaginatedResult<PaymentTransactionDto>>;

public sealed class GetCommerceTransactionsQueryValidator : FluentValidation.AbstractValidator<GetCommerceTransactionsQuery>
{
    public GetCommerceTransactionsQueryValidator()
    {
        RuleFor(x => x.CommerceId).GreaterThan(0).WithMessage("El ID del comercio es requerido.");
        RuleFor(x => x.Page).GreaterThan(0).WithMessage("La página debe ser mayor a 0.");
        RuleFor(x => x.PageSize)
            .GreaterThan(0).WithMessage("El tamaño de página debe ser mayor a 0.")
            .LessThanOrEqualTo(20).WithMessage("El tamaño de página no puede exceder 20.");
    }
}

public sealed class GetCommerceTransactionsQueryHandler(IPaymentProcessorService payments)
    : IRequestHandler<GetCommerceTransactionsQuery, PaginatedResult<PaymentTransactionDto>>
{
    public Task<PaginatedResult<PaymentTransactionDto>> Handle(
        GetCommerceTransactionsQuery request,
        CancellationToken cancellationToken) =>
        payments.GetCommerceTransactionsAsync(request.CommerceId, request.Page, request.PageSize);
}
