using ABP.Core.Domain.Entities;

namespace ABP.Core.Application.Interfaces.IServices
{
    public sealed record InstallmentPaymentAllocation(int InstallmentId, decimal AppliedAmount, bool BecomesPaid);

    public sealed class LoanPaymentAllocationResult
    {
        public IReadOnlyList<InstallmentPaymentAllocation> Allocations { get; init; } = [];
        public decimal TotalApplied { get; init; }
        public bool LoanFullyPaid { get; init; }
    }

    /// <summary>
    /// Applies a loan payment to pending installments following the seniority rule:
    /// the oldest pending installment is paid first, then the next one, until the
    /// payment amount is exhausted or the loan is fully paid (anti-sobrepago).
    /// </summary>
    public interface ILoanPaymentAllocationService
    {
        LoanPaymentAllocationResult Allocate(IEnumerable<LoanInstallment> pendingInstallments, decimal paymentAmount);
    }
}
