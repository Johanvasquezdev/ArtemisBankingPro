using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Xunit;

namespace ABP.Integration.Tests.Repositories;

public sealed class FinancialRepositoryTests : IDisposable
{
    private readonly ArtemisBankingDbContext _context;

    public FinancialRepositoryTests()
    {
        var options = new DbContextOptionsBuilder<ArtemisBankingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ArtemisBankingDbContext(options);
        _context.Database.EnsureCreated();
    }

    [Fact]
    public async Task LoanRepository_ShouldFilterActiveLoansAndCalculateDebt()
    {
        var active = new Loan
        {
            LoanNumber = "300000001",
            ClientId = "client-1",
            Amount = 1000,
            Status = LoanStatus.Active,
            CreatedAt = DateTime.UtcNow,
            Installments =
            [
                new LoanInstallment { InstallmentNumber = 1, DueDate = DateTime.UtcNow.AddDays(-1), InstallmentAmount = 600, AmountPaid = 100, Status = InstallmentStatus.Partial },
                new LoanInstallment { InstallmentNumber = 2, DueDate = DateTime.UtcNow.AddDays(30), InstallmentAmount = 600, AmountPaid = 0, Status = InstallmentStatus.Pending }
            ]
        };
        var closed = new Loan
        {
            LoanNumber = "300000002",
            ClientId = "client-1",
            Amount = 500,
            Status = LoanStatus.Completed,
            CreatedAt = DateTime.UtcNow.AddDays(-1)
        };
        await _context.Loans.AddRangeAsync(active, closed);
        await _context.SaveChangesAsync();

        var repository = new LoanRepository(_context);

        (await repository.ClientHasActiveLoanAsync("client-1")).Should().BeTrue();
        (await repository.GetActiveByClientIdAsync("client-1")).Should().ContainSingle(loan => loan.LoanNumber == "300000001");
        (await repository.GetByLoanNumberAsync("300000001")).Should().NotBeNull();
        (await repository.GetTotalActiveLoansCountAsync()).Should().Be(1);
        (await repository.GetTotalDebtByClientIdAsync("client-1")).Should().Be(1100);
        (await repository.GetAverageDebtAsync()).Should().Be(1100);
    }

    [Fact]
    public async Task LoanRepository_ShouldLoadInstallmentsAndApplyPaging()
    {
        await _context.Loans.AddRangeAsync(
            Loan(1, "300000001", LoanStatus.Active, DateTime.UtcNow.AddMinutes(-2)),
            Loan(2, "300000002", LoanStatus.Active, DateTime.UtcNow.AddMinutes(-1)),
            Loan(3, "300000003", LoanStatus.Completed, DateTime.UtcNow));
        await _context.SaveChangesAsync();

        var repository = new LoanRepository(_context);

        var result = await repository.GetAllPagedAsync(1, 1, LoanStatus.Active);
        var details = await repository.GetByIdAsync(1);

        result.Should().ContainSingle(loan => loan.Id == 2);
        details.Should().NotBeNull();
        details!.Installments.Should().ContainSingle();
    }

    [Fact]
    public async Task LoanInstallmentRepository_ShouldFindPendingAndOverdueInstallments()
    {
        var loan = Loan(1, "300000001", LoanStatus.Active, DateTime.UtcNow);
        loan.Installments =
        [
            new LoanInstallment { InstallmentNumber = 1, DueDate = DateTime.UtcNow.AddDays(-2), InstallmentAmount = 100, AmountPaid = 0, Status = InstallmentStatus.Pending },
            new LoanInstallment { InstallmentNumber = 2, DueDate = DateTime.UtcNow.AddDays(20), InstallmentAmount = 200, AmountPaid = 0, Status = InstallmentStatus.Pending },
            new LoanInstallment { InstallmentNumber = 3, DueDate = DateTime.UtcNow.AddDays(-5), InstallmentAmount = 300, AmountPaid = 300, Status = InstallmentStatus.Paid }
        ];
        await _context.Loans.AddAsync(loan);
        await _context.SaveChangesAsync();

        var repository = new LoanInstallmentRepository(_context);

        (await repository.GetFirstPendingInstallmentAsync(loan.Id))!.InstallmentNumber.Should().Be(1);
        (await repository.GetOverdueInstallmentsAsync()).Should().ContainSingle(item => item.InstallmentNumber == 1);
        (await repository.GetFutureUnpaidInstallmentsAsync(loan.Id)).Should().ContainSingle(item => item.InstallmentNumber == 2);
        (await repository.GetPendingAmountByLoanIdAsync(loan.Id)).Should().Be(300);
        (await repository.GetPaidInstallmentsCountAsync(loan.Id)).Should().Be(1);
    }

    [Fact]
    public async Task CreditCardRepository_ShouldFilterActiveCardsAndCalculateDebt()
    {
        await _context.CreditCards.AddRangeAsync(
            new CreditCard { CardNumber = "4111111111111111", ClientId = "client-1", CreditLimit = 5000, AmountOwed = 1200, Status = CardStatus.Active, CreatedAt = DateTime.UtcNow },
            new CreditCard { CardNumber = "4222222222222222", ClientId = "client-1", CreditLimit = 3000, AmountOwed = 100, Status = CardStatus.Cancelled, CreatedAt = DateTime.UtcNow.AddMinutes(-1) },
            new CreditCard { CardNumber = "4333333333333333", ClientId = "client-2", CreditLimit = 2000, AmountOwed = 800, Status = CardStatus.Active, CreatedAt = DateTime.UtcNow }
        );
        await _context.SaveChangesAsync();

        var repository = new CreditCardRepository(_context);

        (await repository.CardNumberExistsAsync("4111111111111111")).Should().BeTrue();
        (await repository.GetByCardNumberAsync("4111111111111111"))!.AvailableBalance.Should().Be(3800);
        (await repository.GetActiveCardsByClientIdAsync("client-1")).Should().ContainSingle();
        (await repository.GetTotalCardDebtByClientIdAsync("client-1")).Should().Be(1200);
        (await repository.GetTotalActiveCardsCountAsync()).Should().Be(2);
        (await repository.GetAllPagedAsync(1, 1, CardStatus.Active, "client-1")).Should().ContainSingle();
    }

    [Fact]
    public async Task CreditCardConsumptionRepository_ShouldFilterByCardAndCommerce()
    {
        await _context.Consumptions.AddRangeAsync(
            new CreditCardConsumption { CreditCardId = 1, CommerceId = 7, Amount = 100, TransactionDate = DateTime.UtcNow, Status = ConsumptionStatus.Approved },
            new CreditCardConsumption { CreditCardId = 1, CommerceId = 8, Amount = 200, TransactionDate = DateTime.UtcNow.AddMinutes(-1), Status = ConsumptionStatus.Rejected },
            new CreditCardConsumption { CreditCardId = 2, CommerceId = 7, Amount = 300, TransactionDate = DateTime.UtcNow, Status = ConsumptionStatus.Approved }
        );
        await _context.SaveChangesAsync();

        var repository = new CreditCardConsumptionRepository(_context);

        (await repository.GetByCardIdAsync(1)).Should().HaveCount(2);
        (await repository.GetByCommerceIdAsync(7)).Should().HaveCount(2);
        (await repository.GetByCardIdAsync(1)).First().Amount.Should().Be(100);
    }

    [Fact]
    public async Task BeneficiaryRepository_ShouldScopeExistenceToOwner()
    {
        await _context.Beneficiaries.AddRangeAsync(
            new Beneficiary { OwnerId = "client-1", AccountNumber = "100000001", FirstName = "Ana" },
            new Beneficiary { OwnerId = "client-2", AccountNumber = "100000001", FirstName = "Luis" });
        await _context.SaveChangesAsync();

        var repository = new BeneficiaryRepository(_context);

        (await repository.BeneficiaryExistForOwnerAsync("client-1", "100000001")).Should().BeTrue();
        (await repository.BeneficiaryExistForOwnerAsync("client-1", "100000002")).Should().BeFalse();
        (await repository.GetByOwnerAccountIdAsync("client-1")).Should().ContainSingle(item => item.FirstName == "Ana");
    }

    [Fact]
    public async Task IdempotencyRepository_ShouldFindRecordByOperationKeyAndActor()
    {
        var record = new IdempotencyRecord
        {
            Operation = "client.express",
            Key = "request-1",
            ActorUserId = "client-1",
            CreatedAt = DateTime.UtcNow
        };
        await _context.IdempotencyRecords.AddAsync(record);
        await _context.SaveChangesAsync();

        var repository = new IdempotencyRepository(_context);

        (await repository.GetAsync("client.express", "request-1", "client-1")).Should().NotBeNull();
        (await repository.GetAsync("client.express", "request-1", "client-2")).Should().BeNull();
    }

    private static Loan Loan(int id, string number, LoanStatus status, DateTime createdAt) => new()
    {
        Id = id,
        LoanNumber = number,
        ClientId = "client-1",
        Amount = 1000,
        AnualInterestRate = 12,
        TermInMonths = 12,
        Status = status,
        CreatedAt = createdAt,
        Installments =
        [
            new LoanInstallment
            {
                InstallmentNumber = 1,
                DueDate = createdAt.AddDays(30),
                InstallmentAmount = 100,
                AmountPaid = 0,
                Status = InstallmentStatus.Pending
            }
        ]
    };

    public void Dispose()
    {
        _context.Database.EnsureDeleted();
        _context.Dispose();
    }
}
