using System;
using System.Threading.Tasks;
using ABP.Core.Application.Interfaces.Services;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using FluentAssertions;
using Xunit;

namespace ABP.Unit.Tests.Services
{
    public class LoanPaymentAllocationServiceTests
    {
        private readonly LoanPaymentAllocationService _service = new();

        [Fact]
        public void Allocate_ShouldApplyOldestInstallmentFirst()
        {
            var installment1 = Installment(1, 1, 300m, 0m, new DateTime(2026, 1, 1));
            var installment2 = Installment(2, 2, 300m, 0m, new DateTime(2026, 2, 1));

            var result = _service.Allocate([installment1, installment2], 400m);

            result.TotalApplied.Should().Be(400m);
            result.LoanFullyPaid.Should().BeFalse();
            result.Allocations.Should().ContainSingle(a => a.InstallmentId == 1 && a.AppliedAmount == 300m && a.BecomesPaid);
            result.Allocations.Should().ContainSingle(a => a.InstallmentId == 2 && a.AppliedAmount == 100m && !a.BecomesPaid);
        }

        [Fact]
        public void Allocate_ShouldMarkLoanFullyPaid_WhenAmountCoversAll()
        {
            var installment1 = Installment(1, 1, 300m, 0m, new DateTime(2026, 1, 1));
            var installment2 = Installment(2, 2, 200m, 0m, new DateTime(2026, 2, 1));

            var result = _service.Allocate([installment1, installment2], 500m);

            result.TotalApplied.Should().Be(500m);
            result.LoanFullyPaid.Should().BeTrue();
        }

        [Fact]
        public void Allocate_ShouldConsiderPartialAmountPaid()
        {
            var installment1 = Installment(1, 1, 300m, 100m, new DateTime(2026, 1, 1));

            var result = _service.Allocate([installment1], 100m);

            result.TotalApplied.Should().Be(100m);
            result.Allocations.Should().ContainSingle(a => a.InstallmentId == 1 && a.AppliedAmount == 100m && !a.BecomesPaid);
        }

        [Fact]
        public void Allocate_ShouldMarkInstallmentPaid_WhenPaymentCoversRemaining()
        {
            var installment1 = Installment(1, 1, 300m, 100m, new DateTime(2026, 1, 1));

            var result = _service.Allocate([installment1], 200m);

            result.TotalApplied.Should().Be(200m);
            result.Allocations.Should().ContainSingle(a => a.InstallmentId == 1 && a.AppliedAmount == 200m && a.BecomesPaid);
        }

        [Fact]
        public void Allocate_ShouldSkipAlreadyPaidInstallments()
        {
            var paid = Installment(1, 1, 300m, 300m, new DateTime(2026, 1, 1));
            paid.Status = InstallmentStatus.Paid;
            var pending = Installment(2, 2, 300m, 0m, new DateTime(2026, 2, 1));

            var result = _service.Allocate([paid, pending], 300m);

            result.TotalApplied.Should().Be(300m);
            result.Allocations.Should().ContainSingle(a => a.InstallmentId == 2 && a.AppliedAmount == 300m && a.BecomesPaid);
        }

        private static LoanInstallment Installment(int id, int number, decimal amount, decimal paid, DateTime dueDate)
            => new()
            {
                Id = id,
                InstallmentNumber = number,
                InstallmentAmount = amount,
                AmountPaid = paid,
                DueDate = dueDate,
                Status = InstallmentStatus.Pending
            };
    }
}
