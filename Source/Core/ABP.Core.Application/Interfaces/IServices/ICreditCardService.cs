using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Domain.Enums;

namespace ABP.Core.Application.Interfaces.IServices
{
    public interface ICreditCardService
    {
        Task<CreditCardDto> GetByIdAsync(int id);
        Task<CreditCardDto?> GetByCardNumberAsync(string cardNumber);
        Task<bool> VerifyCvcAsync(int cardId, string cvc);
        Task<bool> ChargeAsync(int cardId, decimal amount);
        Task<bool> ChargeWithoutSaveAsync(int cardId, decimal amount);
        Task<IEnumerable<CreditCardDto>> GetActiveByClientIdAsync(string clientId);
        Task<PaginatedResult<CreditCardDto>> GetAllPagedAsync(int page, int pageSize = 20, CardStatus? status = null, string? cedula = null);

        Task<CreditCardDto> AssignAsync(AssignCreditCardDto dto);
        Task<bool> ChangeStatusAsync(int cardId, CardStatus status);

        Task<bool> PayCreditCardAsync(string sourceAccountNumber, string cardNumber, decimal amount);
        Task<bool> CashAdvanceAsync(CashAdvanceDto dto);

        Task<decimal> GetTotalDebtByClientIdAsync(string clientId);
        Task<int> GetTotalActiveCardsCountAsync();
        Task UpdateLimitAsync(int cardId, decimal newCreditLimit);
        Task CancelAsync(int cardId);
    }
}
