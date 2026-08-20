using ABP.Core.Application.DTOs.CreditCardConsumption;
using ABP.Core.Application.DTOs.Payment;
using ABP.Core.Application.DTOs.SavingsAccount;
using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.Interfaces.Services;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using FluentAssertions;
using Moq;
using System.Globalization;
using Xunit;

namespace ABP.Unit.Tests.Services;

public class PaymentProcessorServiceTests
{
    private readonly Mock<ICreditCardService> _cardService = new();
    private readonly Mock<ICommerceService> _commerceService = new();
    private readonly Mock<ICreditCardConsumptionService> _consumptionService = new();
    private readonly Mock<ISavingsAccountService> _accountService = new();
    private readonly Mock<ITransactionRepository> _transactionRepository = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkTransaction> _transaction = new();
    private readonly Mock<IUserReadOnlyService> _userService = new();
    private readonly Mock<IEmailServices> _emailService = new();
    private readonly PaymentProcessorService _service;

    public PaymentProcessorServiceTests()
    {
        _unitOfWork
            .Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()))
            .ReturnsAsync(_transaction.Object);
        _unitOfWork.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>())).ReturnsAsync(0);
        _transaction.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _transaction.Setup(x => x.RollbackAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);
        _transactionRepository.Setup(x => x.AddWithoutSaveAsync(It.IsAny<ABP.Core.Domain.Entities.Transaction>())).Returns(Task.CompletedTask);
        _accountService.Setup(x => x.UpdateWithoutSaveAsync(It.IsAny<SavingsAccountDto>())).Returns(Task.CompletedTask);
        _emailService.Setup(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>())).Returns(Task.CompletedTask);
        _userService.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new UserDto { Email = "user@example.com" });

        _service = new PaymentProcessorService(
            _cardService.Object,
            _commerceService.Object,
            _consumptionService.Object,
            _accountService.Object,
            _transactionRepository.Object,
            _unitOfWork.Object,
            _userService.Object,
            _emailService.Object);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldValidateExpirationAndCvcBeforeCharging()
    {
        var card = BuildCard();
        SetupValidPayment(card);

        var result = await _service.ProcessPaymentAsync(10, BuildPayment(card, cvc: "999"));

        result.Success.Should().BeFalse();
        result.Message.Should().Be("Invalid card security code.");
        _cardService.Verify(x => x.ChargeWithoutSaveAsync(It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldIncreaseDebtCreditCommerceAndRecordConsumption()
    {
        var card = BuildCard();
        SetupValidPayment(card);
        _consumptionService
            .Setup(x => x.AddWithoutSaveAsync(It.IsAny<CreditCardConsumptionDto>()))
            .ReturnsAsync(new CreditCardConsumptionDto { Id = 55, CreditCardId = card.Id, CommerceId = 10, Amount = 100 });

        var result = await _service.ProcessPaymentAsync(10, BuildPayment(card));

        result.Success.Should().BeTrue();
        result.TransactionId.Should().Be(55);
        result.NewBalance.Should().Be(900);
        _cardService.Verify(x => x.ChargeWithoutSaveAsync(card.Id, 100), Times.Once);
        _consumptionService.Verify(x => x.AddWithoutSaveAsync(It.Is<CreditCardConsumptionDto>(c => c.CommerceId == 10 && c.Amount == 100 && c.Status == ConsumptionStatus.Approved)), Times.Once);
        _accountService.Verify(x => x.UpdateWithoutSaveAsync(It.Is<SavingsAccountDto>(a => a.Balance == 600)), Times.Once);
        _transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ProcessPaymentAsync_ShouldRejectWhenCommerceHasNoSettlementAccount()
    {
        var card = BuildCard();
        SetupValidPayment(card);
        _accountService.Setup(x => x.GetPrimaryAccountByClientIdAsync("commerce-user"))
            .ReturnsAsync((SavingsAccountDto?)null);

        var result = await _service.ProcessPaymentAsync(10, BuildPayment(card));

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("settlement account");
        _unitOfWork.Verify(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>()), Times.Once);
        _consumptionService.Verify(x => x.AddWithoutSaveAsync(It.Is<CreditCardConsumptionDto>(c =>
            c.CommerceId == 10 && c.Status == ConsumptionStatus.Rejected)), Times.Once);
    }

    [Fact]
    public async Task GetCommerceTransactionsAsync_ShouldQueryByCommerceId()
    {
        _commerceService.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(new ABP.Core.Application.DTOs.Commerce.CommerceDto { Id = 10, IsActive = true });
        _consumptionService.Setup(x => x.GetByCommerceIdAsync(10)).ReturnsAsync([
            new CreditCardConsumptionDto { Id = 1, CommerceId = 10, Amount = 42, Status = ConsumptionStatus.Approved }
        ]);

        var transactions = await _service.GetCommerceTransactionsAsync(10, 1, 10);

        transactions.Items.Should().ContainSingle().Which.Amount.Should().Be(42);
        _consumptionService.Verify(x => x.GetByCommerceIdAsync(10), Times.Once);
        _consumptionService.Verify(x => x.GetByCardIdAsync(It.IsAny<int>()), Times.Never);
    }

    private void SetupValidPayment(ABP.Core.Application.DTOs.CreditCard.CreditCardDto card)
    {
        _commerceService.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(new ABP.Core.Application.DTOs.Commerce.CommerceDto
        {
            Id = 10,
            Name = "Artemis Store",
            IsActive = true
        });
        _commerceService.Setup(x => x.GetActiveUserIdAsync(10)).ReturnsAsync("commerce-user");
        _cardService.Setup(x => x.GetByCardNumberAsync(card.CardNumber)).ReturnsAsync(card);
        _cardService.Setup(x => x.VerifyCvcAsync(card.Id, "123")).ReturnsAsync(true);
        _cardService.Setup(x => x.ChargeWithoutSaveAsync(card.Id, 100)).ReturnsAsync(true);
        _accountService.Setup(x => x.GetPrimaryAccountByClientIdAsync("commerce-user"))
            .ReturnsAsync(new SavingsAccountDto { Id = 3, AccountNumber = "123456789", Balance = 500, Status = AccountStatus.Active });
    }

    private static ABP.Core.Application.DTOs.CreditCard.CreditCardDto BuildCard() => new()
    {
        Id = 7,
        CardNumber = "4111111111111111",
        CreditLimit = 1000,
        AmountOwed = 0,
        ExpirationDate = DateTime.UtcNow.AddYears(1).ToString("MM/yy"),
        Status = CardStatus.Active,
        ClientId = "card-owner"
    };

    private static ProcessPaymentDto BuildPayment(ABP.Core.Application.DTOs.CreditCard.CreditCardDto card, string cvc = "123")
    {
        var expiration = DateTime.ParseExact(card.ExpirationDate, "MM/yy", CultureInfo.InvariantCulture);
        return new ProcessPaymentDto
        {
            CardNumber = card.CardNumber,
            MonthExpirationCard = expiration.ToString("MM"),
            YearExpirationCard = expiration.ToString("yyyy"),
            CVC = cvc,
            TransactionAmount = 100
        };
    }
}
