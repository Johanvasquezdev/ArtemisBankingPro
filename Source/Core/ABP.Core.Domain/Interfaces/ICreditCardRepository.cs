using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces.IGenerics;

namespace ABP.Core.Domain.Interfaces
{
    public interface ICreditCardRepository : IGenericRepository<CreditCard>
    {
        // Get a credit card by its card number
        Task<CreditCard?> GetByCardNumberAsync(string cardNumber);
        Task<IEnumerable<CreditCard>> GetAllCardsByClientIdAsync(string clientId);
        Task<bool> CardNumberExistsAsync(string cardNumber);
        Task<decimal> GetTotalCardDebtByClientIdAsync(string clientId);
        Task<IEnumerable<CreditCard>> GetActiveCardsByClientIdAsync(string clientId);
        Task<IEnumerable<CreditCard>> GetAllPagedAsync(int page, int pageSize, CardStatus? status = null, string? clientId = null);
        Task<int> GetTotalActiveCardsCountAsync();
        Task<int> GetFilteredCountAsync(CardStatus? status = null, string? clientId = null);
    }
}
