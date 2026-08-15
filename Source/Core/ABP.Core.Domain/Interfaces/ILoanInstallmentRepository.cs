using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Interfaces.IGenerics;

namespace ABP.Core.Domain.Interfaces
{
    public interface ILoanInstallmentRepository : IGenericRepository<LoanInstallment>
    {
        Task<int> GetPaidInstallmentsCountAsync(int loanId);
        Task<decimal> GetPendingAmountByLoanIdAsync(int loanId);
        Task<IEnumerable<LoanInstallment>> GetByLoanIdAsync(int loanId);
        Task<IEnumerable<LoanInstallment>> GetByLoanIdsAsync(IEnumerable<int> loanIds);
        // get all installments that are overdue and not fully paid
        Task<IEnumerable<LoanInstallment>> GetOverdueInstallmentsAsync();
        Task<LoanInstallment?> GetFirstPendingInstallmentAsync(int loanId);
        Task<IEnumerable<LoanInstallment>> GetFutureUnpaidInstallmentsAsync(int loanId);
    }
}
