using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.DTOs.Commerce;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.Features.Admin.Commands;
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

public sealed class AdminFinancialWorkflowIntegrationTests : IDisposable
{
    private readonly ArtemisBankingDbContext _context;
    private readonly IMapper _mapper;
    private readonly TestUnitOfWork _unitOfWork;
    private readonly Mock<IUserReadOnlyService> _users = new();

    public AdminFinancialWorkflowIntegrationTests()
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
    public async Task AssignCreditCardCommand_ShouldPersistActiveCardWithGeneratedSecurityData()
    {
        var service = CreateCreditCardService();
        var handler = new AssignCreditCardCommandHandler(service);

        var result = await handler.Handle(
            new AssignCreditCardCommand(new AssignCreditCardDto
            {
                ClientId = "client-1",
                CreditLimit = 5000
            }), CancellationToken.None);

        result.Id.Should().BeGreaterThan(0);
        result.CreditLimit.Should().Be(5000);
        result.CardNumber.Should().HaveLength(19);
        result.CardNumber.Should().MatchRegex(@"^\*\*\*\* \*\*\*\* \*\*\*\* \d{4}$");
        result.ExpirationDate.Should().MatchRegex(@"^\d{2}/\d{2}$");

        var persisted = await _context.CreditCards.SingleAsync();
        persisted.Status.Should().Be(CardStatus.Active);
        persisted.AmountOwed.Should().Be(0);
        persisted.CVCHash.Should().HaveLength(64);
    }

    [Fact]
    public async Task AssignSecondaryAccountCommand_ShouldPersistAccountAndInitialTransaction()
    {
        await AddPrimaryAccountAsync("client-1", 1000);
        var service = CreateSavingsAccountService();
        var handler = new AssignSecondarySavingsAccountCommandHandler(service);

        var result = await handler.Handle(
            new AssignSecondarySavingsAccountCommand(new AssignSavingsAccountDto
            {
                ClientId = "client-1",
                AdminId = "admin-1",
                InitialBalance = 250
            }), CancellationToken.None);

        result.Success.Should().BeTrue();
        var account = await _context.Savings.SingleAsync(account => account.Type == AccountType.Secondary);
        account.Balance.Should().Be(250);
        account.Status.Should().Be(AccountStatus.Active);

        var transaction = await _context.Transactions.SingleAsync();
        transaction.Amount.Should().Be(250);
        transaction.SavingAccountId.Should().Be(account.Id);
        transaction.Type.Should().Be(TransactionType.Credit);
    }

    [Fact]
    public async Task AssignSecondaryAccountCommand_ShouldRejectClientWithoutPrimaryAccount()
    {
        var service = CreateSavingsAccountService();
        var handler = new AssignSecondarySavingsAccountCommandHandler(service);

        var action = () => handler.Handle(
            new AssignSecondarySavingsAccountCommand(new AssignSavingsAccountDto
            {
                ClientId = "client-without-primary",
                AdminId = "admin-1",
                InitialBalance = 250
            }), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cuenta principal activa*");
        (await _context.Savings.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task AssignLoanCommand_ShouldPersistInstallmentsAndDisburseToPrimaryAccount()
    {
        var primary = await AddPrimaryAccountAsync("client-1", 1000);
        var service = CreateLoanService();
        var handler = new AssignLoanCommandHandler(service);

        var result = await handler.Handle(
            new AssignLoanCommand(new AssignLoanDto
            {
                ClientId = "client-1",
                AdminId = "admin-1",
                Amount = 1200,
                AnnualInterestRate = 12,
                TermInMonths = 12
            }, ConfirmHighRisk: true), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Loan.Should().NotBeNull();

        var loan = await _context.Loans.Include(item => item.Installments).SingleAsync();
        loan.Status.Should().Be(LoanStatus.Active);
        loan.Installments.Should().HaveCount(12);
        loan.Installments.Should().OnlyContain(item => item.Status == InstallmentStatus.Pending);

        var account = await _context.Savings.SingleAsync(item => item.Id == primary.Id);
        account.Balance.Should().Be(2200);
        var disbursement = await _context.Transactions.SingleAsync();
        disbursement.Amount.Should().Be(1200);
        disbursement.DestinationAccountNumber.Should().Be(primary.AccountNumber);
    }

    [Fact]
    public async Task AssignLoanCommand_ShouldFailWithoutPrimaryAccountAndPersistNothing()
    {
        var service = CreateLoanService();
        var handler = new AssignLoanCommandHandler(service);

        var action = () => handler.Handle(
            new AssignLoanCommand(new AssignLoanDto
            {
                ClientId = "client-without-primary",
                AdminId = "admin-1",
                Amount = 1200,
                AnnualInterestRate = 12,
                TermInMonths = 12
            }, ConfirmHighRisk: true), CancellationToken.None);

        await action.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cuenta principal activa*");
        (await _context.Loans.AnyAsync()).Should().BeFalse();
        (await _context.Installments.AnyAsync()).Should().BeFalse();
        (await _context.Transactions.AnyAsync()).Should().BeFalse();
    }

    [Fact]
    public async Task CreateCommerce_ShouldNormalizeDataAndRejectDuplicateRnc()
    {
        var service = new CommerceService(
            new CommerceRepository(_context),
            _mapper,
            new Mock<ICommerceUserDirectory>().Object,
            _unitOfWork);

        var first = await service.AddAsync(new CommerceDto
        {
            Name = "  Artemis Store ",
            Description = "  Test commerce ",
            Logo = "  logo.svg ",
            Rnc = "101-010101",
            Email = " STORE@TEST.LOCAL "
        });

        first.Name.Should().Be("Artemis Store");
        first.Rnc.Should().Be("101010101");
        first.Email.Should().Be("store@test.local");

        var duplicate = () => service.AddAsync(new CommerceDto
        {
            Name = "Other",
            Description = "Other",
            Logo = "logo.svg",
            Rnc = "101010101",
            Email = "other@test.local"
        });

        await duplicate.Should().ThrowAsync<Exception>()
            .WithMessage("*RNC*");
        (await _context.Commerces.CountAsync()).Should().Be(1);
    }

    private CreditCardService CreateCreditCardService()
        => new(
            new CreditCardRepository(_context),
            new CreditCardConsumptionRepository(_context),
            new SavingsAccountRepository(_context),
            _mapper,
            _users.Object,
            new Mock<IEmailServices>().Object,
            NullLogger<CreditCardService>.Instance,
            _unitOfWork);

    private SavingsAccountService CreateSavingsAccountService()
        => new(
            new SavingsAccountRepository(_context),
            _mapper,
            _users.Object,
            new TransactionRepository(_context),
            _unitOfWork);

    private LoanService CreateLoanService()
        => new(
            new LoanRepository(_context, new Moq.Mock<ABP.Core.Application.Interfaces.IServices.IUserReadOnlyService>().Object),
            new LoanInstallmentRepository(_context),
            new TransactionRepository(_context),
            new SavingsAccountRepository(_context),
            _users.Object,
            _mapper,
            _unitOfWork);

    private async Task<SavingsAccount> AddPrimaryAccountAsync(string clientId, decimal balance)
    {
        var account = new SavingsAccount
        {
            UserId = clientId,
            AccountNumber = $"{Random.Shared.Next(100000000, 999999999)}",
            Balance = balance,
            Type = AccountType.Primary,
            Status = AccountStatus.Active,
            CreatedAt = DateTime.UtcNow,
            CreatedByAdminId = "admin-1"
        };

        await _context.Savings.AddAsync(account);
        await _context.SaveChangesAsync();
        return account;
    }

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
