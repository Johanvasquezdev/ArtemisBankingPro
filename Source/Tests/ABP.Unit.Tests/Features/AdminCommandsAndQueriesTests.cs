using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.DTOs.Commerce;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.DTOs.SavingsAccount;
using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Features.Admin.Commands;
using ABP.Core.Application.Features.Admin.Queries;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using FluentAssertions;
using Moq;
using Xunit;

namespace ABP.Unit.Tests.Features;

public class AdminCommandValidatorTests
{
    [Fact]
    public void CreateCommerceValidator_ShouldRejectInvalidRncAndEmail()
    {
        var result = new CreateCommerceCommandValidator().Validate(new CreateCommerceCommand(
            "Commerce",
            "test en crear comercio comando",
            "logo.png",
            "invalid",
            "1231231100",
            "123",
            "admin-1"
        ));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Commerce.Rnc");
        result.Errors.Should().Contain(error => error.PropertyName == "Commerce.Email");
    }

    [Fact]
    public void AssignLoanValidator_ShouldRejectUnsupportedTerm()
    {
        var result = new AssignLoanCommandValidator().Validate(new AssignLoanCommand(new AssignLoanDto
        {
            ClientId = "client-1",
            AdminId = "admin-1",
            Amount = 1000,
            AnnualInterestRate = 12,
            TermInMonths = 7
        }));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "Loan.TermInMonths");
    }

    [Fact]
    public void AssignSecondaryByCedulaValidator_ShouldRejectNegativeBalance()
    {
        var result = new AssignSecondarySavingsAccountByCedulaCommandValidator()
            .Validate(new AssignSecondarySavingsAccountByCedulaCommand("40200000000", -1, "admin-1"));

        result.IsValid.Should().BeFalse();
    }

    [Fact]
    public void AssignCreditCardValidator_ShouldRejectNonPositiveLimit()
    {
        var result = new AssignCreditCardCommandValidator().Validate(new AssignCreditCardCommand(new AssignCreditCardDto
        {
            ClientId = "client-1",
            CreditLimit = 0
        }));

        result.IsValid.Should().BeFalse();
    }
}

public class AdminCommandHandlerTests
{
    [Fact]
    public async Task CreateCommerceHandler_ShouldReturnServiceResult()
    {
        var service = new Mock<ICommerceService>();
        var expected = new CommerceDto 
        { 
            Id = 7, 
            Name = "Commerce",
            Description = "test", 
            Rnc = "123456789", 
            PhoneNumber = "1234567890", 
            Email = "commerce@test.local",
            Logo = "logo.png",
            CreatedByAdminId = "admin-1",
        };
        service.Setup(x => x.AddAsync(It.IsAny<CommerceDto>())).ReturnsAsync(expected);

        var result = await new CreateCommerceCommandHandler(service.Object).Handle(
            new CreateCommerceCommand(
                expected.Name,
                expected.Description,
                expected.Rnc,
                expected.PhoneNumber,
                expected.Email,
                expected.Logo,
                expected.CreatedByAdminId
            ),
            CancellationToken.None
        );

        result.Should().BeEquivalentTo(expected);
        service.Verify(x => x.AddAsync(It.Is<CommerceDto>(d => d.Name == expected.Name && d.Rnc == expected.Rnc)), Times.Once);
        service.Verify(x => x.AddAsync(expected), Times.Once);
    }

    [Fact]
    public async Task AssignCreditCardHandler_ShouldDelegateToService()
    {
        var service = new Mock<ICreditCardService>();
        var dto = new AssignCreditCardDto { ClientId = "client-1", CreditLimit = 5000 };
        var expected = new CreditCardDto { Id = 2, CreditLimit = 5000 };
        service.Setup(x => x.AssignAsync(dto)).ReturnsAsync(expected);

        var result = await new AssignCreditCardCommandHandler(service.Object)
            .Handle(new AssignCreditCardCommand(dto), CancellationToken.None);

        result.Should().BeSameAs(expected);
        service.Verify(x => x.AssignAsync(dto), Times.Once);
    }

    [Fact]
    public async Task AssignLoanHandler_ShouldRequestConfirmationBeforeWritingHighRiskLoan()
    {
        var service = new Mock<ILoanService>();
        service.Setup(x => x.ClientHasActiveLoanAsync("client-1")).ReturnsAsync(false);
        service.Setup(x => x.EvaluateRiskAsync("client-1", 1000, 12, 12))
            .ReturnsAsync((true, 500m, 800m));

        var result = await new AssignLoanCommandHandler(service.Object).Handle(
            new AssignLoanCommand(new AssignLoanDto
            {
                ClientId = "client-1", AdminId = "admin-1", Amount = 1000,
                AnnualInterestRate = 12, TermInMonths = 12
            }), CancellationToken.None);

        result.RequiresRiskConfirmation.Should().BeTrue();
        result.Succeeded.Should().BeFalse();
        service.Verify(x => x.AssignAsync(It.IsAny<AssignLoanDto>()), Times.Never);
    }

    [Fact]
    public async Task AssignLoanHandler_ShouldAssignWhenRiskWasConfirmed()
    {
        var service = new Mock<ILoanService>();
        var dto = new AssignLoanDto
        {
            ClientId = "client-1", AdminId = "admin-1", Amount = 1000,
            AnnualInterestRate = 12, TermInMonths = 12
        };
        var expected = new LoanDto { Id = 9, ClientId = "client-1" };
        service.Setup(x => x.ClientHasActiveLoanAsync("client-1")).ReturnsAsync(false);
        service.Setup(x => x.EvaluateRiskAsync("client-1", 1000, 12, 12))
            .ReturnsAsync((true, 500m, 800m));
        service.Setup(x => x.AssignAsync(dto)).ReturnsAsync(expected);

        var result = await new AssignLoanCommandHandler(service.Object)
            .Handle(new AssignLoanCommand(dto, ConfirmHighRisk: true), CancellationToken.None);

        result.Succeeded.Should().BeTrue();
        result.Loan.Should().BeSameAs(expected);
        service.Verify(x => x.AssignAsync(dto), Times.Once);
    }

    [Fact]
    public async Task AssignSecondaryByCedulaHandler_ShouldResolveClientAndDelegate()
    {
        var users = new Mock<IUserReadOnlyService>();
        var accounts = new Mock<ISavingsAccountService>();
        users.Setup(x => x.GetActiveClientsAsync("40200000000"))
            .ReturnsAsync(new[] { new UserDto { Id = "client-1", Cedula = "40200000000", IsActive = true } });
        accounts.Setup(x => x.GetPrimaryAccountByClientIdAsync("client-1"))
            .ReturnsAsync(new SavingsAccountDto { AccountNumber = "100000000" });

        await new AssignSecondarySavingsAccountByCedulaCommandHandler(users.Object, accounts.Object).Handle(
            new AssignSecondarySavingsAccountByCedulaCommand("40200000000", 100, "admin-1"), CancellationToken.None);

        accounts.Verify(x => x.AssignSecondaryAsync(It.Is<AssignSavingsAccountDto>(dto =>
            dto.ClientId == "client-1" && dto.AdminId == "admin-1" && dto.InitialBalance == 100)), Times.Once);
    }
}

public class AdminQueryHandlerTests
{
    [Fact]
    public async Task GetAdminCommercesQueryHandler_ShouldPassStatusFilter()
    {
        var service = new Mock<ICommerceService>();
        var expected = new PaginatedResult<CommerceDto> { Page = 1, PageSize = 20, TotalCount = 1 };
        service.Setup(x => x.GetAllPagedAsync(1, 20, false)).ReturnsAsync(expected);

        var result = await new GetAdminCommercesQueryHandler(service.Object)
            .Handle(new GetAdminCommercesQuery(1, 20, false), CancellationToken.None);

        result.Should().BeSameAs(expected);
        service.Verify(x => x.GetAllPagedAsync(1, 20, false), Times.Once);
    }
}
