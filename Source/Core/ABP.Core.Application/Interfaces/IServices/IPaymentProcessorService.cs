using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.Payment;

namespace ABP.Core.Application.Interfaces.IServices
{
    public interface IPaymentProcessorService
    {
        Task<PaymentResultDto> ProcessPaymentAsync(int commerceId, ProcessPaymentDto paymentDto);
        Task<PaginatedResult<PaymentTransactionDto>> GetCommerceTransactionsAsync(int commerceId, int pageNumber, int pageSize);
    }
}
