using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using ABP.Infraestructure.identity.Context;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infraestructure.Persistence.Repositories
{
    public class CreditCardRepository(ArtemisBankDbContext context, IdentityContext identityContext) : GenericRepository<CreditCard>(context), ICreditCardRepository
    {
        private readonly IdentityContext _identityContext = identityContext;
        public async Task<bool> CardNumberExistsAsync(string cardNumber)
        {
            return await _dbSet.AnyAsync(cc => cc.CardNumber == cardNumber);
        }

        public async Task<IEnumerable<CreditCard>> GetActiveCardsByClientIdAsync(string clientId)
        {
            return await _dbSet.Where(cc => cc.ClientId == clientId && cc.Status == CardStatus.Active)
                .OrderByDescending(cc => cc.CreatedAt).ToListAsync();
        }

        public async Task<IEnumerable<CreditCard>> GetAllCardsByClientCedulaAsync(string cedula)
        {
            var clientId = await _identityContext.Users.Where(u => u.Cedula == cedula).Select(u => u.Id).FirstOrDefaultAsync();

            if (clientId == null)
            {
                return [];
            }

            return await _dbSet.AsNoTracking().Where(cc => cc.ClientId == clientId)
               .OrderByDescending(cc => cc.Status == CardStatus.Active).ThenByDescending(cc => cc.CreatedAt)
               .ToListAsync();
        }

        public async Task<IEnumerable<CreditCard>> GetAllPagedAsync(int page, int pageSize, CardStatus? status = null, string? cedula = null)
        {
            var query = _dbSet.AsNoTracking().AsQueryable();

            if (!string.IsNullOrEmpty(cedula))
            {
                var clientId = await _identityContext.Users.Where(u => u.Cedula == cedula).Select(u => u.Id).FirstOrDefaultAsync();
                if (clientId != null)
                {
                    query = query.Where(cc => cc.ClientId == clientId);
                }
                else
                {
                    return [];
                }
            }

            if (status.HasValue)
            {
                query = query.Where(cc => cc.Status == status);
            }

            return await query.OrderByDescending(cc => cc.Status == CardStatus.Active).ThenByDescending(cc => cc.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        public async Task<CreditCard?> GetByCardNumberAsync(string cardNumber)
        {
            return await _dbSet.FirstOrDefaultAsync(cc => cc.CardNumber == cardNumber);
        }

        public async Task<int> GetTotalActiveCardsCountAsync()
        {
            return await _dbSet.CountAsync(cc => cc.Status == CardStatus.Active);
        }

        public async Task<decimal> GetTotalCardDebtByClientIdAsync(string clientId)
        {
            return await _dbSet.Where(cc => cc.ClientId == clientId && cc.Status == CardStatus.Active)
                .SumAsync(cc => cc.AmountOwed);
        }
    }
}
