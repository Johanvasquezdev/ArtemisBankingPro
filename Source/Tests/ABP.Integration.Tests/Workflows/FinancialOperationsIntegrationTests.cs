using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.Interfaces.Services;
using ABP.Core.Application.Mappings;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.Persistence.Repositories;
using AutoMapper;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ABP.Integration.Tests.Workflows;

public sealed class FinancialOperationsIntegrationTests : IDisposable
{
    private readonly ArtemisBankingDbContext _context;
    private readonly IMapper _mapper;
    private readonly TestUnitOfWork _unitOfWork;
    private readonly Mock<IUserReadOnlyService> _users = new();

    public FinancialOperationsIntegrationTests()
    {
        var options = new DbContextOptionsBuilder<ArtemisBankingDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;

        _context = new ArtemisBankingDbContext(options);
        _mapper = new MapperConfiguration(
            configuration => configuration.AddProfile<AutoMapperProfile>(),
            NullLoggerFactory.Instance).CreateMapper();
        _unitOfWork = new TestUnitOfWork(_context);
    }

    [Fact]
    public async Task SavingsTransfer_ShouldMoveFundsBetweenActiveAccounts()
    {
        await AddAccountAsync("source", "100000001", 1000, AccountType.Primary);
        await AddAccountAsync("destination", "100000002", 100, AccountType.Secondary);
        var service = new SavingsAccountService(
            new SavingsAccountRepository(_context), _mapper, _users.Object,
            new TransactionRepository(_context), _unitOfWork);

        var result = await service.TransferAsync("100000001", "100000002", 250);

        result.Should().BeTrue();
        (await BalanceAsync("100000001")).Should().Be(750);
        (await BalanceAsync("100000002")).Should().Be(350);
    }

    [Fact]
    public async Task CreditCardPayment_ShouldReduceDebtAndDebitSourceAccount()
    {
        await AddAccountAsync("client-1", "100000003", 1000, AccountType.Primary);
        await _context.CreditCards.AddAsync(new CreditCard
        {
            ClientId = "client-1",
            CardNumber = "4111111111111111",
            CreditLimit = 5000,
            AmountOwed = 300,
            CVCHash = "hash",
            ExpirationDate = "12/30",
            Status = CardStatus.Active,
            CreatedAt = DateTime.UtcNow,
            AssignedByAdminId = "admin-1"
        });
        await _context.SaveChangesAsync();

        var service = new CreditCardService(
            new CreditCardRepository(_context),
            new CreditCardConsumptionRepository(_context),
            new SavingsAccountRepository(_context),
            _mapper,
            _users.Object,
            new Mock<IEmailServices>().Object,
            NullLogger<CreditCardService>.Instance,
            _unitOfWork);

        var result = await service.PayCreditCardAsync("100000003", "4111111111111111", 200);

        result.Should().BeTrue();
        (await BalanceAsync("100000003")).Should().Be(800);
        (await _context.CreditCards.Select(card => card.AmountOwed).SingleAsync()).Should().Be(100);
    }

    [Fact]
    public async Task LoanPayment_ShouldApplyPartialPaymentToTheOldestInstallment()
    {
        await AddAccountAsync("client-2", "100000004", 1000, AccountType.Primary);
        var loan = new Loan
        {
            ClientId = "client-2",
            LoanNumber = "300000001",
            Amount = 1000,
            AnualInterestRate = 12,
            TermInMonths = 2,
            Status = LoanStatus.Active,
            CreatedAt = DateTime.UtcNow,
            AssignedByAdminId = "admin-1",
            Installments =
            [
                new LoanInstallment
                {
                    InstallmentNumber = 1,
                    DueDate = DateTime.UtcNow.AddDays(-1),
                    InstallmentAmount = 300,
                    AmountPaid = 0,
                    Status = InstallmentStatus.Pending
                },
                new LoanInstallment
                {
                    InstallmentNumber = 2,
                    DueDate = DateTime.UtcNow.AddDays(30),
                    InstallmentAmount = 300,
                    AmountPaid = 0,
                    Status = InstallmentStatus.Pending
                }
            ]
        };
        await _context.Loans.AddAsync(loan);
        await _context.SaveChangesAsync();

        var mockUserService = new Moq.Mock<ABP.Core.Application.Interfaces.IServices.IUserReadOnlyService>();
        var loanRepo = new LoanRepository(_context, mockUserService.Object);
        var service = new LoanService(
            loanRepo,
            new LoanInstallmentRepository(_context),
            new TransactionRepository(_context),
            new SavingsAccountRepository(_context),
            _users.Object,
            _mapper,
            new Mock<IEmailServices>().Object,
            NullLogger<LoanService>.Instance,
            _unitOfWork);

        var result = await service.PayLoanInstallmentAsync("100000004", "300000001", 150);

        result.Should().BeTrue();
        (await BalanceAsync("100000004")).Should().Be(850);
        var installments = await _context.Installments.OrderBy(item => item.InstallmentNumber).ToListAsync();
        installments[0].AmountPaid.Should().Be(150);
        installments[0].Status.Should().Be(InstallmentStatus.Pending);
        installments[1].AmountPaid.Should().Be(0);
    }

    [Fact]
    public async Task LoanLateFeeCommand_ShouldMarkOnlyActiveOverdueLoansAndClearPaidOnes()
    {
        var active = new Loan
        {
            ClientId = "client-3", LoanNumber = "300000002", Amount = 1000,
            AnualInterestRate = 12, TermInMonths = 2, Status = LoanStatus.Active,
            CreatedAt = DateTime.UtcNow, AssignedByAdminId = "admin-1",
            Installments =
            [
                new LoanInstallment
                {
                    InstallmentNumber = 1, DueDate = DateTime.UtcNow.AddDays(-2),
                    InstallmentAmount = 300, AmountPaid = 0,
                    Status = InstallmentStatus.Pending, IsOverdue = false
                },
                new LoanInstallment
                {
                    InstallmentNumber = 2, DueDate = DateTime.UtcNow.AddDays(-3),
                    InstallmentAmount = 300, AmountPaid = 300,
                    Status = InstallmentStatus.Paid, IsOverdue = true
                }
            ]
        };
        var completed = new Loan
        {
            ClientId = "client-4", LoanNumber = "300000003", Amount = 1000,
            AnualInterestRate = 12, TermInMonths = 2, Status = LoanStatus.Completed,
            CreatedAt = DateTime.UtcNow, AssignedByAdminId = "admin-1",
            Installments =
            [
                new LoanInstallment
                {
                    InstallmentNumber = 1, DueDate = DateTime.UtcNow.AddDays(-2),
                    InstallmentAmount = 300, AmountPaid = 0,
                    Status = InstallmentStatus.Pending, IsOverdue = false
                }
            ]
        };
        await _context.Loans.AddRangeAsync(active, completed);
        await _context.SaveChangesAsync();

        var handler = new ABP.Core.Application.Features.Functions.Commands.RunLoanLateFeeAndInterestCommandHandler(
            new LoanInstallmentRepository(_context), new LoanRepository(_context, new Moq.Mock<ABP.Core.Application.Interfaces.IServices.IUserReadOnlyService>().Object), _unitOfWork);

        var result = await handler.Handle(
            new ABP.Core.Application.Features.Functions.Commands.RunLoanLateFeeAndInterestCommand(),
            CancellationToken.None);

        result.MarkedOverdue.Should().Be(1);
        result.ClearedOverdue.Should().Be(1);
        var activeInstallments = await _context.Installments
            .Where(item => item.LoanId == active.Id)
            .OrderBy(item => item.InstallmentNumber)
            .ToListAsync();
        activeInstallments[0].IsOverdue.Should().BeTrue();
        activeInstallments[1].IsOverdue.Should().BeFalse();
        (await _context.Installments.SingleAsync(item => item.LoanId == completed.Id)).IsOverdue.Should().BeFalse();
    }

    private async Task AddAccountAsync(string userId, string number, decimal balance, AccountType type)
    {
        await _context.Savings.AddAsync(new SavingsAccount
        {
            UserId = userId,
            AccountNumber = number,
            Balance = balance,
            Type = type,
            Status = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedByAdminId = "admin-1"
        });
        await _context.SaveChangesAsync();
    }

    private Task<decimal> BalanceAsync(string accountNumber)
        => _context.Savings.Where(account => account.AccountNumber == accountNumber)
            .Select(account => account.Balance)
            .SingleAsync();

    public void Dispose() => _context.Dispose();

    private sealed class TestUnitOfWork(ArtemisBankingDbContext context) : IUnitOfWork
    {
        public Task<int> SaveChangesAsync(CancellationToken cancellationToken = default)
            => context.SaveChangesAsync(cancellationToken);

        public Task<IUnitOfWorkTransaction> BeginTransactionAsync(CancellationToken cancellationToken = default)
            => Task.FromResult<IUnitOfWorkTransaction>(new NoOpTransaction());
    }

    private sealed class NoOpTransaction : IUnitOfWorkTransaction
    {
        public Task CommitAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public Task RollbackAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
        public ValueTask DisposeAsync() => ValueTask.CompletedTask;
    }
}
