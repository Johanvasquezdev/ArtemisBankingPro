using ABP.Core.Application.DTOs.Payment;
using ABP.Core.Application.Interfaces.IServices;
using MediatR;

namespace ABP.Core.Application.Features.Commerce.Queries;

public sealed record GetCommerceTransactionsQuery(int CommerceId)
    : IRequest<IEnumerable<PaymentTransactionDto>>;

public sealed class GetCommerceTransactionsQueryHandler(IPaymentProcessorService payments)
    : IRequestHandler<GetCommerceTransactionsQuery, IEnumerable<PaymentTransactionDto>>
{
    public Task<IEnumerable<PaymentTransactionDto>> Handle(
        GetCommerceTransactionsQuery request,
        CancellationToken cancellationToken) =>
        payments.GetCommerceTransactionsAsync(request.CommerceId);
}
