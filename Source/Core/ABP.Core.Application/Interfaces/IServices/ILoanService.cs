using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.DTOs.User;
using ABP.Core.Domain.Enums;

namespace ABP.Core.Application.Interfaces.IServices
{
    public interface ILoanService
    {
        Task<LoanDto> GetByIdAsync(int id);
        Task<LoanDto?> GetByLoanNumberAsync(string loanNumber);
        Task<IEnumerable<LoanDto>> GetActiveByClientIdAsync(string clientId);
        Task<PaginatedResult<LoanDto>> GetAllPagedAsync(int page, int pageSize = 20, LoanStatus? status = null, string? cedula = null);

        Task<LoanDto> AssignAsync(AssignLoanDto dto);
        Task<IEnumerable<UserDto>> GetActiveClientsWithoutLoanAsync(string? cedula = null);
        Task<bool> PayLoanInstallmentAsync(string sourceAccountNumber, string loanNumber, decimal amount);

        Task<bool> ClientHasActiveLoanAsync(string clientId);
        Task<decimal> GetTotalDebtByClientIdAsync(string clientId);
        Task<decimal> GetAverageDebtAsync();
        Task<int> GetTotalActiveLoansCountAsync();
        Task<(bool IsHighRisk, decimal AverageDebt, decimal CurrentDebt)> EvaluateRiskAsync(string clientId, decimal amount, decimal rate, int months);
        Task UpdateInterestRateAsync(int loanId, decimal newAnnualInterestRate);
    }
}
