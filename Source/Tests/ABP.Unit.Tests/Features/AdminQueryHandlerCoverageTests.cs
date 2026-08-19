using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.CreditCardConsumption;
using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.DTOs.SavingsAccount;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Features.Admin.Queries;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using FluentAssertions;
using Moq;
using Xunit;

namespace ABP.Unit.Tests.Features;

public sealed class AdminQueryHandlerCoverageTests
{
    [Fact]
    public async Task GetCommerceUsersHandler_ShouldForwardPagination()
    {
        var users = new Mock<IUserReadOnlyService>();
        var expected = new PaginatedResult<UserDto> { Page = 2, PageSize = 10, TotalCount = 1, Items = [new UserDto { Id = "commerce-1" }] };
        users.Setup(x => x.GetCommerceUsersAsync(2, 10)).ReturnsAsync(expected);

        var result = await new GetCommerceUsersQueryHandler(users.Object)
            .Handle(new GetCommerceUsersQuery(2, 10), CancellationToken.None);

        result.Should().BeSameAs(expected);
        users.Verify(x => x.GetCommerceUsersAsync(2, 10), Times.Once);
    }

    [Fact]
    public async Task GetCreditCardByIdHandler_ShouldComposeCardAndConsumptions()
    {
        var cards = new Mock<ICreditCardService>();
        var consumptions = new Mock<ICreditCardConsumptionService>();
        var card = new CreditCardDto { Id = 8, CreditLimit = 5000 };
        var items = new[] { new CreditCardConsumptionDto { Id = 3, CreditCardId = 8, Amount = 100 } };
        cards.Setup(x => x.GetByIdAsync(8)).ReturnsAsync(card);
        consumptions.Setup(x => x.GetByCardIdAsync(8)).ReturnsAsync(items);

        var result = await new GetCreditCardByIdQueryHandler(cards.Object, consumptions.Object)
            .Handle(new GetCreditCardByIdQuery(8), CancellationToken.None);

        result.Should().NotBeNull();
        result!.Card.Should().BeSameAs(card);
        result.Consumptions.Should().BeEquivalentTo(items);
    }

    [Fact]
    public async Task GetCreditCardByIdHandler_ShouldReturnNullWhenCardDoesNotExist()
    {
        var cards = new Mock<ICreditCardService>();
        var consumptions = new Mock<ICreditCardConsumptionService>();
        cards.Setup(x => x.GetByIdAsync(99)).ReturnsAsync((CreditCardDto)null!);

        var result = await new GetCreditCardByIdQueryHandler(cards.Object, consumptions.Object)
            .Handle(new GetCreditCardByIdQuery(99), CancellationToken.None);

        result.Should().BeNull();
        consumptions.Verify(x => x.GetByCardIdAsync(It.IsAny<int>()), Times.Never);
    }

    [Fact]
    public async Task GetCreditCardsHandler_ShouldTranslateStatusFilter()
    {
        var cards = new Mock<ICreditCardService>();
        var expected = new PaginatedResult<CreditCardDto>();
        cards.Setup(x => x.GetAllPagedAsync(1, 20, CardStatus.Cancelled, "402"))
            .ReturnsAsync(expected);

        var result = await new GetCreditCardsQueryHandler(cards.Object)
            .Handle(new GetCreditCardsQuery(1, 20, "cancelada", "402"), CancellationToken.None);

        result.Should().BeSameAs(expected);
        cards.Verify(x => x.GetAllPagedAsync(1, 20, CardStatus.Cancelled, "402"), Times.Once);
    }

    [Fact]
    public async Task GetLoanByIdHandler_ShouldDelegateToLoanService()
    {
        var loans = new Mock<ILoanService>();
        var expected = new LoanDto { Id = 4 };
        loans.Setup(x => x.GetByIdAsync(4)).ReturnsAsync(expected);

        var result = await new GetLoanByIdQueryHandler(loans.Object)
            .Handle(new GetLoanByIdQuery(4), CancellationToken.None);

        result.Should().BeSameAs(expected);
    }

    [Fact]
    public async Task GetLoansHandler_ShouldTranslateStatusAndIdentification()
    {
        var loans = new Mock<ILoanService>();
        var expected = new PaginatedResult<LoanDto>();
        loans.Setup(x => x.GetAllPagedAsync(2, 5, LoanStatus.Completed, "40200000000"))
            .ReturnsAsync(expected);

        var result = await new GetLoansQueryHandler(loans.Object)
            .Handle(new GetLoansQuery(2, 5, "completados", "40200000000"), CancellationToken.None);

        result.Should().BeSameAs(expected);
        loans.Verify(x => x.GetAllPagedAsync(2, 5, LoanStatus.Completed, "40200000000"), Times.Once);
    }

    [Fact]
    public async Task GetSavingsAccountsHandler_ShouldTranslateStatusAndType()
    {
        var accounts = new Mock<ISavingsAccountService>();
        var expected = new PaginatedResult<SavingsAccountDto>();
        accounts.Setup(x => x.GetAllPagedAsync(1, 20, AccountStatus.Closed, AccountType.Secondary, "402"))
            .ReturnsAsync(expected);

        var result = await new GetSavingsAccountsQueryHandler(accounts.Object)
            .Handle(new GetSavingsAccountsQuery(1, 20, "402", "cancelada", "secundaria"), CancellationToken.None);

        result.Should().BeSameAs(expected);
        accounts.Verify(x => x.GetAllPagedAsync(1, 20, AccountStatus.Closed, AccountType.Secondary, "402"), Times.Once);
    }

    [Fact]
    public async Task GetSavingsAccountTransactionsHandler_ShouldPageAndReturnNullForUnknownAccount()
    {
        var accounts = new Mock<ISavingsAccountService>();
        var account = new SavingsAccountDto { AccountNumber = "123456789" };
        var transactions = Enumerable.Range(1, 3).Select(id => new TransactionDto { Id = id }).ToArray();
        accounts.Setup(x => x.GetByAccountNumberAsync(account.AccountNumber)).ReturnsAsync(account);
        accounts.Setup(x => x.GetTransactionsAsync(account.AccountNumber)).ReturnsAsync(transactions);

        var result = await new GetSavingsAccountTransactionsQueryHandler(accounts.Object)
            .Handle(new GetSavingsAccountTransactionsQuery(account.AccountNumber, 2, 2), CancellationToken.None);

        result.Should().NotBeNull();
        result!.TotalRecords.Should().Be(3);
        result.Data.Should().ContainSingle(item => item.Id == 3);

        accounts.Setup(x => x.GetByAccountNumberAsync("999999999")).ReturnsAsync((SavingsAccountDto?)null);
        (await new GetSavingsAccountTransactionsQueryHandler(accounts.Object)
            .Handle(new GetSavingsAccountTransactionsQuery("999999999"), CancellationToken.None))
            .Should().BeNull();
    }

    [Fact]
    public async Task GetUserByIdHandler_ShouldDelegateToUserReadOnlyService()
    {
        var users = new Mock<IUserReadOnlyService>();
        var expected = new UserDto { Id = "user-1" };
        users.Setup(x => x.GetByIdAsync("user-1")).ReturnsAsync(expected);

        var result = await new GetUserByIdQueryHandler(users.Object)
            .Handle(new GetUserByIdQuery("user-1"), CancellationToken.None);

        result.Should().BeSameAs(expected);
    }
}
