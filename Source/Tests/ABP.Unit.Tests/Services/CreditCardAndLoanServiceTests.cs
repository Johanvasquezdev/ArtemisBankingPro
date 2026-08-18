using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.CreditCardConsumption;
using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.Interfaces.Services;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using Moq;
using Xunit;

namespace ABP.Unit.Tests.Services;

public sealed class CreditCardServiceTests
{
    private readonly Mock<ICreditCardRepository> _cards = new();
    private readonly Mock<ICreditCardConsumptionRepository> _consumptions = new();
    private readonly Mock<ISavingsAccountRepository> _accounts = new();
    private readonly Mock<AutoMapper.IMapper> _mapper = new();
    private readonly Mock<IUserReadOnlyService> _users = new();
    private readonly Mock<IEmailServices> _email = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkTransaction> _transaction = new();

    private CreditCardService CreateService() => new(
        _cards.Object, _consumptions.Object, _accounts.Object, _mapper.Object, _users.Object,
        _email.Object, NullLogger<CreditCardService>.Instance, _unitOfWork.Object);

    [Fact]
    public async Task ChargeWithoutSave_ShouldIncreaseDebtOnlyWhenCreditIsAvailable()
    {
        var card = new CreditCard { Id = 7, CreditLimit = 1000, AmountOwed = 200, Status = CardStatus.Active };
        _cards.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(card);

        var result = await CreateService().ChargeWithoutSaveAsync(7, 300);

        result.Should().BeTrue();
        card.AmountOwed.Should().Be(500);
        _cards.Verify(x => x.UpdateWithoutSaveAsync(card), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task CashAdvance_ShouldApplyInterestCreditAccountAndRecordConsumption()
    {
        var card = new CreditCard { Id = 7, CreditLimit = 1000, AmountOwed = 100, Status = CardStatus.Active };
        var account = new SavingsAccount { Id = 3, Balance = 50, Status = AccountStatus.Active };
        _cards.Setup(x => x.GetByIdAsync(7)).ReturnsAsync(card);
        _accounts.Setup(x => x.GetByIdAsync(3)).ReturnsAsync(account);
        _unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_transaction.Object);

        var result = await CreateService().CashAdvanceAsync(new CashAdvanceDto
        {
            ClientId = "client-1", CreditCardId = 7, SavingsAccountId = 3, Amount = 200
        });

        result.Should().BeTrue();
        card.AmountOwed.Should().Be(312.50m);
        account.Balance.Should().Be(250);
        _consumptions.Verify(x => x.AddWithoutSaveAsync(It.Is<CreditCardConsumption>(c =>
            c.CreditCardId == 7 && c.Amount == 200 && c.Status == ConsumptionStatus.Approved)), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}

public sealed class LoanServiceValidationTests
{
    [Fact]
    public async Task Assign_ShouldRejectMissingPrimaryAccountBeforeOpeningTransaction()
    {
        var loans = new Mock<ILoanRepository>();
        var installments = new Mock<ILoanInstallmentRepository>();
        var transactions = new Mock<ITransactionRepository>();
        var accounts = new Mock<ISavingsAccountRepository>();
        var users = new Mock<IUserReadOnlyService>();
        var mapper = new Mock<AutoMapper.IMapper>();
        var unitOfWork = new Mock<IUnitOfWork>();
        loans.Setup(x => x.ClientHasActiveLoanAsync("client-1")).ReturnsAsync(false);
        loans.Setup(x => x.GetByLoanNumberAsync(It.IsAny<string>())).ReturnsAsync((Loan?)null);
        accounts.Setup(x => x.GetPrimaryAccountByClientIdAsync("client-1"))
            .ReturnsAsync((SavingsAccount?)null);

        var act = () => new LoanService(loans.Object, installments.Object, transactions.Object,
            accounts.Object, users.Object, mapper.Object, unitOfWork.Object).AssignAsync(new AssignLoanDto
            {
                ClientId = "client-1", AdminId = "admin-1", Amount = 1000,
                AnnualInterestRate = 12, TermInMonths = 12
            });

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cuenta principal activa*");
        unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Never);
        loans.Verify(x => x.AddWithoutSaveAsync(It.IsAny<Loan>()), Times.Never);
    }
}
