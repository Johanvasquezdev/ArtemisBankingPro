using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories.Generic;
using Microsoft.EntityFrameworkCore;

namespace ABP.Infraestructure.Persistence.Repositories
{
    public class LoanRepository(ArtemisBankingDbContext context, ABP.Core.Application.Interfaces.IServices.IUserReadOnlyService userService) : GenericRepository<Loan>(context), ILoanRepository
    {
        private readonly ABP.Core.Application.Interfaces.IServices.IUserReadOnlyService _userService = userService;

        public override async Task<Loan?> GetByIdAsync(int id)
        {
            return await _context.Loans
                .Include(l => l.Installments)
                .FirstOrDefaultAsync(l => l.Id == id);
        }
        public async Task<bool> ClientHasActiveLoanAsync(string clientId)
        {
            return await _dbSet.AnyAsync(l => l.ClientId == clientId && l.Status == LoanStatus.Active);
        }

        public async Task<IEnumerable<Loan>> GetActiveByClientIdAsync(string clientId)
        {
            return await _dbSet.AsNoTracking()
                .Where(l => l.ClientId == clientId && l.Status == LoanStatus.Active)
                .OrderByDescending(l => l.CreatedAt).ToListAsync();
        }

        public async Task<IEnumerable<string>> GetActiveLoanClientIdsAsync()
        {
            return await _dbSet.AsNoTracking()
                .Where(l => l.Status == LoanStatus.Active)
                .Select(l => l.ClientId)
                .Distinct()
                .ToListAsync();
        }

        public async Task<Loan?> GetActiveLoanByClientIdAsync(string clientId)
        {
            return await _dbSet.Include(l => l.Installments).FirstOrDefaultAsync(l => l.ClientId == clientId && l.Status == LoanStatus.Active);
        }

        public async Task<IEnumerable<Loan>> GetAllByClientIdAsync(string clientId)
        {
            return await _dbSet.AsNoTracking().Where(l => l.ClientId == clientId)
                .OrderByDescending(l => l.Status == LoanStatus.Active).ThenByDescending(l=> l.CreatedAt).ToListAsync();
        }

        public async Task<IEnumerable<Loan>> GetAllPagedAsync(int page, int pageSize, LoanStatus? status = null, string? clientId = null)
        {
            var query = _dbSet.AsNoTracking().Include(l => l.Installments).AsQueryable();

            if (!string.IsNullOrEmpty(clientId))
            {
                query = query.Where(l => l.ClientId == clientId);
            }

            if (status.HasValue)
            {
                query = query.Where(l => l.Status == status);
            }

            return await query.OrderByDescending(l => l.Status == LoanStatus.Active).ThenByDescending(l => l.CreatedAt)
                .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
        }

        public async Task<decimal> GetAverageDebtAsync()
        {
            // average debt = (total debt of active loans + total debt of active credit cards) / number of active clients
            var totalLoanDebt = await _dbSet.Where(l => l.Status == LoanStatus.Active)
                .SelectMany(l => l.Installments).Where(i => i.Status != InstallmentStatus.Paid)
                .SumAsync(i => i.InstallmentAmount - i.AmountPaid);

            var totalCardDebt = await _context.CreditCards.Where(cc => cc.Status == CardStatus.Active)
                .SumAsync(cc => cc.AmountOwed);

            var activeClientsCount = await _userService.GetActiveClientsCountAsync();

            if (activeClientsCount == 0) return 0;

            return (totalLoanDebt + totalCardDebt) / activeClientsCount;
        }

        public async Task<Loan?> GetByLoanNumberAsync(string loanNumber)
        {
            return await _dbSet.FirstOrDefaultAsync(l => l.LoanNumber == loanNumber);
        }

        public async Task<int> GetTotalActiveLoansCountAsync()
        {
            return await _dbSet.CountAsync(l => l.Status == LoanStatus.Active);
        }

        public async Task<int> GetFilteredCountAsync(LoanStatus? status = null, string? clientId = null)
        {
            var query = _dbSet.AsQueryable();
            if (status.HasValue)
                query = query.Where(l => l.Status == status.Value);
            if (!string.IsNullOrWhiteSpace(clientId))
                query = query.Where(l => l.ClientId == clientId);
            return await query.CountAsync();
        }

        public async Task<decimal> GetTotalDebtByClientIdAsync(string clientId)
        {
            // total debt = pending amount  + debt in credit cards
            var loanDebt = await _dbSet.Where(l => l.ClientId == clientId && l.Status == LoanStatus.Active)
                .SelectMany(l => l.Installments).Where(i => i.Status != InstallmentStatus.Paid)
                .SumAsync(i => i.InstallmentAmount - i.AmountPaid);

            var cardDebt = await _context.CreditCards.Where(cc => cc.ClientId == clientId && cc.Status == CardStatus.Active)
                .SumAsync(cc => cc.AmountOwed);

            return loanDebt + cardDebt;
        }
    }
}
