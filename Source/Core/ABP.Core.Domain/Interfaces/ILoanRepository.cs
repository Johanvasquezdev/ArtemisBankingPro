using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces.IGenerics;

namespace ABP.Core.Domain.Interfaces
{
    public interface ILoanRepository : IGenericRepository<Loan>
    {
        Task<Loan?> GetByLoanNumberAsync(string loanNumber);
        Task<Loan?> GetActiveLoanByClientIdAsync(string clientId);
        // check if a customer already has an active loan
        Task<bool> ClientHasActiveLoanAsync(string clientId);
        Task<IEnumerable<Loan>> GetActiveByClientIdAsync(string clientId);
        Task<IEnumerable<string>> GetActiveLoanClientIdsAsync();
        Task<IEnumerable<Loan>> GetAllByClientIdAsync(string clientId);
        // calculate the average debt of all customers in the system
        Task<decimal> GetAverageDebtAsync();
        Task<int> GetTotalActiveLoansCountAsync();
        // obtain the current total debt of a specific customer
        Task<decimal> GetTotalDebtByClientIdAsync(string clientId);
        // get list of loans with pagination for the admin
        Task<IEnumerable<Loan>> GetAllPagedAsync(int page, int pageSize, LoanStatus? status = null, string? clientId = null);
        Task<int> GetFilteredCountAsync(LoanStatus? status = null, string? clientId = null);
    }
}
