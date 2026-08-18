using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.CreditCardConsumption;
using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.DTOs.LoanInstallment;
using ABP.Core.Application.DTOs.SavingsAccount;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Features.Admin.Queries;
using ABP.Core.Application.Features.Cashier.Queries;
using ABP.Core.Application.Features.Client.Queries;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.ViewModels.Client;
using ABP.Core.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace ABP.Unit.Tests.Features;

public sealed class AdminQueryCoverageTests
{
    [Fact]
    public async Task UserQueries_ShouldDelegateToReadOnlyService()
    {
        var users = new Mock<IUserReadOnlyService>();
        var page = new PaginatedResult<UserDto> { Page = 2, PageSize = 10, TotalCount = 1 };
        var user = new UserDto { Id = "user-1" };
        users.Setup(x => x.GetAllAsync(2, 10, UserRole.Client)).ReturnsAsync(page);
        users.Setup(x => x.GetCommerceUsersAsync(2, 10)).ReturnsAsync(page);
        users.Setup(x => x.GetByIdAsync("user-1")).ReturnsAsync(user);
        users.Setup(x => x.ExistsByCedulaAsync("40200000000", "user-1")).ReturnsAsync(true);
        users.Setup(x => x.GetActiveClientsAsync("40200000000")).ReturnsAsync(new[] { user });

        (await new GetAdminUsersQueryHandler(users.Object).Handle(new(2, 10, UserRole.Client), CancellationToken.None)).Should().BeSameAs(page);
        (await new GetAdminCommerceUsersQueryHandler(users.Object).Handle(new(2, 10), CancellationToken.None)).Should().BeSameAs(page);
        (await new GetAdminUserQueryHandler(users.Object).Handle(new("user-1"), CancellationToken.None)).Should().BeSameAs(user);
        (await new CheckUserCedulaQueryHandler(users.Object).Handle(new("40200000000", "user-1"), CancellationToken.None)).Should().BeTrue();
        (await new GetActiveClientsQueryHandler(users.Object).Handle(new("40200000000"), CancellationToken.None)).Should().ContainSingle();
    }

    [Fact]
    public async Task SavingsQueries_ShouldReturnDetailsAndTransactions()
    {
        var accounts = new Mock<ISavingsAccountService>();
        var account = new SavingsAccountDto { Id = 1, AccountNumber = "100000001" };
        var transactions = new[] { new TransactionDto { Id = 5, Amount = 100 } };
        var page = new PaginatedResult<SavingsAccountDto> { Page = 1, PageSize = 20, TotalCount = 1 };
        accounts.Setup(x => x.GetAllPagedAsync(1, 20, AccountStatus.Active, AccountType.Secondary, "40200000000")).ReturnsAsync(page);
        accounts.Setup(x => x.GetByAccountNumberAsync("100000001")).ReturnsAsync(account);
        accounts.Setup(x => x.GetPrimaryAccountByClientIdAsync("user-1")).ReturnsAsync(account);
        accounts.Setup(x => x.GetTransactionsAsync("100000001")).ReturnsAsync(transactions);

        (await new GetAdminSavingsAccountsQueryHandler(accounts.Object)
            .Handle(new(1, 20, AccountStatus.Active, AccountType.Secondary, "40200000000"), CancellationToken.None)).Should().BeSameAs(page);
        (await new GetAdminSavingsAccountQueryHandler(accounts.Object)
            .Handle(new("100000001"), CancellationToken.None)).Should().BeSameAs(account);
        (await new GetPrimarySavingsAccountQueryHandler(accounts.Object)
            .Handle(new("user-1"), CancellationToken.None)).Should().BeSameAs(account);

        var detail = await new GetAdminSavingsAccountTransactionsQueryHandler(accounts.Object)
            .Handle(new("100000001"), CancellationToken.None);

        detail.Should().NotBeNull();
        detail!.Account.Should().BeSameAs(account);
        detail.Transactions.Should().ContainSingle(transaction => transaction.Id == 5);
    }

    [Fact]
    public async Task SavingsTransactionsQuery_ShouldReturnNullForUnknownAccount()
    {
        var accounts = new Mock<ISavingsAccountService>();
        accounts.Setup(x => x.GetByAccountNumberAsync("missing")).ReturnsAsync((SavingsAccountDto?)null);

        var result = await new GetAdminSavingsAccountTransactionsQueryHandler(accounts.Object)
            .Handle(new("missing"), CancellationToken.None);

        result.Should().BeNull();
        accounts.Verify(x => x.GetTransactionsAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task CreditCardQueries_ShouldReturnPagedAndDetailedData()
    {
        var cards = new Mock<ICreditCardService>();
        var consumptions = new Mock<ICreditCardConsumptionService>();
        var card = new CreditCardDto { Id = 1, CardNumber = "****1111" };
        var page = new PaginatedResult<CreditCardDto> { Page = 1, PageSize = 20, TotalCount = 1 };
        cards.Setup(x => x.GetAllPagedAsync(1, 20, CardStatus.Active, "40200000000")).ReturnsAsync(page);
        cards.Setup(x => x.GetByIdAsync(1)).ReturnsAsync(card);
        consumptions.Setup(x => x.GetByCardIdAsync(1)).ReturnsAsync(new[] { new CreditCardConsumptionDto { Id = 3 } });

        (await new GetAdminCreditCardsQueryHandler(cards.Object)
            .Handle(new(1, 20, CardStatus.Active, "40200000000"), CancellationToken.None)).Should().BeSameAs(page);

        var details = await new GetAdminCreditCardDetailsQueryHandler(cards.Object, consumptions.Object)
            .Handle(new(1), CancellationToken.None);

        details.Should().NotBeNull();
        details!.Card.Should().BeSameAs(card);
        details.Consumptions.Should().ContainSingle(consumption => consumption.Id == 3);
    }

    [Fact]
    public async Task CreditCardDetailsQuery_ShouldReturnNullBeforeLoadingConsumptions()
    {
        var cards = new Mock<ICreditCardService>();
        var consumptions = new Mock<ICreditCardConsumptionService>();
        cards.Setup(x => x.GetByIdAsync(9)).ReturnsAsync((CreditCardDto)null!);

        var result = await new GetAdminCreditCardDetailsQueryHandler(cards.Object, consumptions.Object)
            .Handle(new(9), CancellationToken.None);

        result.Should().BeNull();
        consumptions.Verify(x => x.GetByCardIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task LoanQueries_ShouldDelegatePagedAndRiskOperations()
    {
        var loans = new Mock<ILoanService>();
        var installments = new Mock<ILoanInstallmentService>();
        var page = new PaginatedResult<LoanDto> { Page = 1, PageSize = 20, TotalCount = 1 };
        var loan = new LoanDto { Id = 2, LoanNumber = "300000001" };
        var client = new UserDto { Id = "client-1" };
        loans.Setup(x => x.GetAllPagedAsync(1, 20, LoanStatus.Active, "40200000000")).ReturnsAsync(page);
        loans.Setup(x => x.GetByIdAsync(2)).ReturnsAsync(loan);
        loans.Setup(x => x.GetActiveClientsWithoutLoanAsync("40200000000")).ReturnsAsync(new[] { client });
        loans.Setup(x => x.GetAverageDebtAsync()).ReturnsAsync(500);
        loans.Setup(x => x.ClientHasActiveLoanAsync("client-1")).ReturnsAsync(false);
        loans.Setup(x => x.EvaluateRiskAsync("client-1", 1000, 12, 12)).ReturnsAsync((true, 500m, 1000m));
        installments.Setup(x => x.GetByLoanIdAsync(2)).ReturnsAsync(new[] { new LoanInstallmentDto { Id = 4, LoanId = 2 } });

        (await new GetAdminLoansQueryHandler(loans.Object).Handle(new(1, 20, LoanStatus.Active, "40200000000"), CancellationToken.None)).Should().BeSameAs(page);
        (await new GetAdminLoanQueryHandler(loans.Object).Handle(new(2), CancellationToken.None)).Should().BeSameAs(loan);

        var details = await new GetAdminLoanDetailsQueryHandler(loans.Object, installments.Object)
            .Handle(new(2), CancellationToken.None);
        details.Should().NotBeNull();
        details!.Installments.Should().ContainSingle(installment => installment.Id == 4);

        var options = await new GetAdminLoanAssignmentOptionsQueryHandler(loans.Object)
            .Handle(new("40200000000"), CancellationToken.None);
        options.Clients.Should().ContainSingle();
        options.AverageDebt.Should().Be(500);

        (await new GetClientActiveLoanQueryHandler(loans.Object).Handle(new("client-1"), CancellationToken.None)).Should().BeFalse();
        (await new EvaluateLoanRiskQueryHandler(loans.Object).Handle(new("client-1", 1000, 12, 12), CancellationToken.None)).IsHighRisk.Should().BeTrue();
    }

    [Fact]
    public async Task LoanDetailsQuery_ShouldReturnNullForUnknownLoan()
    {
        var loans = new Mock<ILoanService>();
        var installments = new Mock<ILoanInstallmentService>();
        loans.Setup(x => x.GetByIdAsync(10)).ReturnsAsync((LoanDto)null!);

        var result = await new GetAdminLoanDetailsQueryHandler(loans.Object, installments.Object)
            .Handle(new(10), CancellationToken.None);

        result.Should().BeNull();
        installments.Verify(x => x.GetByLoanIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task DashboardQueries_ShouldDelegateToDashboardService()
    {
        var dashboard = new Mock<IDashboardService>();
        var admin = new ABP.Core.Application.DTOs.Dashboard.DashboardAdminDto { TotalTransactions = 5 };
        var cashier = new ABP.Core.Application.DTOs.Dashboard.DashboardCashierDto { TodayTransactions = 2 };
        dashboard.Setup(x => x.GetAdminDashboardAsync()).ReturnsAsync(admin);
        dashboard.Setup(x => x.GetCashierDashboardAsync("cashier-1")).ReturnsAsync(cashier);

        (await new GetAdminDashboardQueryHandler(dashboard.Object).Handle(new(), CancellationToken.None)).Should().BeSameAs(admin);
        (await new GetCashierDashboardQueryHandler(dashboard.Object).Handle(new("cashier-1"), CancellationToken.None)).Should().BeSameAs(cashier);
    }

    [Fact]
    public async Task CashierHistoryQuery_ShouldDelegateTake()
    {
        var transactions = new Mock<ITransactionQueryService>();
        var expected = new[] { new TransactionDto { Id = 1 } };
        transactions.Setup(x => x.GetHistoryAsync(25)).ReturnsAsync(expected);

        var result = await new GetCashierHistoryQueryHandler(transactions.Object)
            .Handle(new(25), CancellationToken.None);

        result.Should().BeSameAs(expected);
        transactions.Verify(x => x.GetHistoryAsync(25), Times.Once);
    }

    [Fact]
    public async Task CommerceQuery_ShouldReturnCommerceDetails()
    {
        var commerces = new Mock<ICommerceService>();
        var expected = new ABP.Core.Application.DTOs.Commerce.CommerceDto { Id = 8, Name = "Artemis Shop" };
        commerces.Setup(x => x.GetByIdAsync(8)).ReturnsAsync(expected);

        var result = await new GetAdminCommerceQueryHandler(commerces.Object)
            .Handle(new(8), CancellationToken.None);

        result.Should().BeSameAs(expected);
        commerces.Verify(x => x.GetByIdAsync(8), Times.Once);
    }
}

public sealed class ClientQueryCoverageTests
{
    [Fact]
    public async Task AccountDetailQuery_ShouldFilterTransactionsByDateAndOwner()
    {
        var accounts = new Mock<ISavingsAccountService>();
        var transactions = new Mock<ITransactionQueryService>();
        var users = new Mock<IUserReadOnlyService>();
        var account = new SavingsAccountDto
        {
            Id = 1,
            AccountNumber = "100000001",
            UserId = "client-1",
            Balance = 1000,
            Status = AccountStatus.Active,
            Type = AccountType.Primary
        };
        accounts.Setup(x => x.GetByAccountNumberAsync("100000001")).ReturnsAsync(account);
        transactions.Setup(x => x.GetByAccountIdAsync(1)).ReturnsAsync(new[]
        {
            new TransactionDto { Id = 1, TransactionDate = new DateTime(2026, 1, 10) },
            new TransactionDto { Id = 2, TransactionDate = new DateTime(2026, 2, 10) }
        });
        users.Setup(x => x.GetByIdAsync("client-1")).ReturnsAsync(new UserDto { FirstName = "Johan", LastName = "Vasquez" });

        var result = await new GetAccountDetailQueryHandler(accounts.Object, transactions.Object, users.Object)
            .Handle(new("client-1", "100000001", new DateTime(2026, 2, 1), new DateTime(2026, 2, 28)), CancellationToken.None);

        result.OwnerFullName.Should().Be("Johan Vasquez");
        result.Transactions.Should().ContainSingle(transaction => transaction.Id == 2);
    }

    [Fact]
    public async Task BeneficiariesQuery_ShouldMapFullName()
    {
        var beneficiaries = new Mock<IBeneficiaryService>();
        beneficiaries.Setup(x => x.GetByOwnerIdAsync("client-1")).ReturnsAsync(new[]
        {
            new ABP.Core.Application.DTOs.Beneficiary.BeneficiaryDto { Id = 1, AccountNumber = "100000002", FirstName = "Ana", LastName = "Perez" }
        });

        var result = await new GetBeneficiariesQueryHandler(beneficiaries.Object)
            .Handle(new("client-1"), CancellationToken.None);

        result.Should().ContainSingle(item => item.FullName == "Ana Perez" && item.AccountNumber == "100000002");
    }

    [Fact]
    public async Task TransactionOptionsQuery_ShouldKeepOnlyActiveAccountsAndMapBeneficiaries()
    {
        var accounts = new Mock<ISavingsAccountService>();
        var cards = new Mock<ICreditCardService>();
        var loans = new Mock<ILoanService>();
        var beneficiaries = new Mock<IBeneficiaryService>();
        accounts.Setup(x => x.GetByClientIdAsync("client-1")).ReturnsAsync(new[]
        {
            new SavingsAccountDto { Id = 1, Status = AccountStatus.Active },
            new SavingsAccountDto { Id = 2, Status = AccountStatus.Closed }
        });
        cards.Setup(x => x.GetActiveByClientIdAsync("client-1")).ReturnsAsync(new[] { new CreditCardDto { Id = 3 } });
        loans.Setup(x => x.GetActiveByClientIdAsync("client-1")).ReturnsAsync(new[] { new LoanDto { Id = 4 } });
        beneficiaries.Setup(x => x.GetByOwnerIdAsync("client-1")).ReturnsAsync(new[]
        {
            new ABP.Core.Application.DTOs.Beneficiary.BeneficiaryDto { Id = 5, FirstName = "Ana", LastName = "Perez" }
        });

        var result = await new GetTransactionOptionsQueryHandler(accounts.Object, cards.Object, loans.Object, beneficiaries.Object)
            .Handle(new("client-1"), CancellationToken.None);

        result.Accounts.Should().ContainSingle(account => account.Id == 1);
        result.CreditCards.Should().ContainSingle(card => card.Id == 3);
        result.Loans.Should().ContainSingle(loan => loan.Id == 4);
        result.Beneficiaries.Should().ContainSingle(item => item.FullName == "Ana Perez");
    }

    [Fact]
    public async Task ClientHomeQuery_ShouldAggregateLoansAndRecentTransactions()
    {
        var users = new Mock<IUserReadOnlyService>();
        var accounts = new Mock<ISavingsAccountService>();
        var cards = new Mock<ICreditCardService>();
        var loans = new Mock<ILoanService>();
        var installments = new Mock<ILoanInstallmentService>();
        var transactions = new Mock<ITransactionQueryService>();
        users.Setup(x => x.GetByIdAsync("client-1")).ReturnsAsync(new UserDto { FirstName = "Johan", LastName = "Vasquez" });
        accounts.Setup(x => x.GetByClientIdAsync("client-1")).ReturnsAsync(new[] { new SavingsAccountDto { Id = 1, Status = AccountStatus.Active } });
        cards.Setup(x => x.GetActiveByClientIdAsync("client-1")).ReturnsAsync(new[] { new CreditCardDto { Id = 2 } });
        loans.Setup(x => x.GetActiveByClientIdAsync("client-1")).ReturnsAsync(new[] { new LoanDto { Id = 3, LoanNumber = "300000001", Status = LoanStatus.Active } });
        installments.Setup(x => x.GetByLoanIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(new[]
        {
            new LoanInstallmentDto { LoanId = 3, InstallmentAmount = 100, AmountPaid = 50, Status = InstallmentStatus.Pending, IsOverdue = true, DueDate = DateTime.UtcNow.AddDays(-1) }
        });
        transactions.Setup(x => x.GetByAccountIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(new[]
        {
            new TransactionDto { Id = 1, TransactionDate = DateTime.UtcNow }
        });

        var result = await new GetClientHomeQueryHandler(users.Object, accounts.Object, cards.Object, loans.Object, installments.Object, transactions.Object)
            .Handle(new("client-1"), CancellationToken.None);

        result.ClientFullName.Should().Be("Johan Vasquez");
        result.TotalAccounts.Should().Be(1);
        result.TotalCreditCards.Should().Be(1);
        result.TotalLoans.Should().Be(1);
        result.OverdueInstallmentsCount.Should().Be(1);
        result.HasDelinquentLoans.Should().BeTrue();
        result.RecentTransactions.Should().ContainSingle();
    }
}
