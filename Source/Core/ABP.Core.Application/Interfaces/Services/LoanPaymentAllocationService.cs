using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;

namespace ABP.Core.Application.Interfaces.Services
{
    public class LoanPaymentAllocationService : ILoanPaymentAllocationService
    {
        public LoanPaymentAllocationResult Allocate(IEnumerable<LoanInstallment> pendingInstallments, decimal paymentAmount)
        {
            var ordered = pendingInstallments
                .Where(i => i.Status != InstallmentStatus.Paid)
                .OrderBy(i => i.InstallmentNumber)
                .ThenBy(i => i.DueDate)
                .ToList();

            var allocations = new List<InstallmentPaymentAllocation>();
            var remaining = paymentAmount;
            var totalApplied = 0m;

            foreach (var installment in ordered)
            {
                if (remaining <= 0) break;

                var needed = installment.InstallmentAmount - installment.AmountPaid;
                if (needed <= 0) continue;

                var applied = Math.Min(remaining, needed);
                var becomesPaid = installment.AmountPaid + applied >= installment.InstallmentAmount;

                allocations.Add(new InstallmentPaymentAllocation(installment.Id, applied, becomesPaid));

                remaining -= applied;
                totalApplied += applied;
            }

            var fullyPaid = ordered.Count > 0 && allocations.Count == ordered.Count && remaining >= 0 &&
                            ordered.All(i => allocations.Any(a => a.InstallmentId == i.Id && a.BecomesPaid));

            return new LoanPaymentAllocationResult
            {
                Allocations = allocations,
                TotalApplied = totalApplied,
                LoanFullyPaid = fullyPaid
            };
        }
    }
}
