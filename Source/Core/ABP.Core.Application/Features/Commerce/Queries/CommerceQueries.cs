using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.Payment;
using ABP.Core.Application.Interfaces.IServices;
using MediatR;

namespace ABP.Core.Application.Features.Commerce.Queries;

public sealed record GetCommerceTransactionsQuery(int CommerceId, int PageNumber = 1, int PageSize = 10)
    : IRequest<PaginatedResult<PaymentTransactionDto>>;

public sealed class GetCommerceTransactionsQueryHandler(IPaymentProcessorService payments)
    : IRequestHandler<GetCommerceTransactionsQuery, PaginatedResult<PaymentTransactionDto>>
{
    public Task<PaginatedResult<PaymentTransactionDto>> Handle(
        GetCommerceTransactionsQuery request,
        CancellationToken cancellationToken) =>
        payments.GetCommerceTransactionsAsync(request.CommerceId, request.PageNumber, request.PageSize);
}
