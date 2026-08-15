using ABP.Core.Application.DTOs.LoanInstallment;

namespace ABP.Core.Application.Interfaces.IServices
{
    public interface ILoanInstallmentService
    {
        Task<LoanInstallmentDto> GetByIdAsync(int id);
        Task<IEnumerable<LoanInstallmentDto>> GetByLoanIdAsync(int loanId);
        Task<IEnumerable<LoanInstallmentDto>> GetByLoanIdsAsync(IEnumerable<int> loanIds);
        Task<LoanInstallmentDto?> GetFirstPendingAsync(int loanId);
        Task<decimal> GetPendingAmountByLoanIdAsync(int loanId);
        Task<int> GetPaidCountAsync(int loanId);
        Task<bool> PayInstallmentAsync(int installmentId, decimal amount);
        Task<IEnumerable<LoanInstallmentDto>> GetOverdueInstallmentsAsync();
    }
}
