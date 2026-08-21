using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.CreditCardConsumption;
using ABP.Core.Application.DTOs.Payment;
using ABP.Core.Application.DTOs.SavingsAccount;
using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.Interfaces.Services;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using FluentAssertions;
using Moq;
using System.Globalization;
using Xunit;

namespace ABP.Unit.Tests.Services;

public sealed class IdempotencyServiceTests
{
    [Fact]
    public async Task HermesPay_ShouldRejectDuplicateKeyBeforeChargingCard()
    {
        var cards = new Mock<ICreditCardService>();
        var commerces = new Mock<ICommerceService>();
        var consumptions = new Mock<ICreditCardConsumptionService>();
        var accounts = new Mock<ISavingsAccountService>();
        var transactions = new Mock<ITransactionRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var transaction = new Mock<IUnitOfWorkTransaction>();
        var users = new Mock<IUserReadOnlyService>();
        var email = new Mock<IEmailServices>();
        var idempotency = new Mock<IIdempotencyRepository>();
        var card = new CreditCardDto
        {
            Id = 7, CardNumber = "4111111111111111", CreditLimit = 1000, AmountOwed = 0,
            ExpirationDate = DateTime.UtcNow.AddYears(1).ToString("MM/yy"), Status = CardStatus.Active,
            ClientId = "card-owner"
        };
        var payment = new ProcessPaymentDto
        {
            CardNumber = card.CardNumber,
            MonthExpirationCard = DateTime.ParseExact(card.ExpirationDate, "MM/yy", CultureInfo.InvariantCulture).ToString("MM"),
            YearExpirationCard = DateTime.ParseExact(card.ExpirationDate, "MM/yy", CultureInfo.InvariantCulture).ToString("yyyy"),
            CVC = "123", TransactionAmount = 100, IdempotencyKey = " duplicate-key "
        };

        commerces.Setup(x => x.GetByIdAsync(10)).ReturnsAsync(new ABP.Core.Application.DTOs.Commerce.CommerceDto
        {
            Id = 10, Name = "Artemis Store", IsActive = true
        });
        commerces.Setup(x => x.GetActiveUserIdAsync(10)).ReturnsAsync("commerce-user");
        cards.Setup(x => x.GetByCardNumberAsync(card.CardNumber)).ReturnsAsync(card);
        cards.Setup(x => x.VerifyCvcAsync(card.Id, "123")).ReturnsAsync(true);
        accounts.Setup(x => x.GetPrimaryAccountByClientIdAsync("commerce-user"))
            .ReturnsAsync(new SavingsAccountDto { Id = 3, AccountNumber = "123456789", Balance = 500, Status = AccountStatus.Active });
        idempotency.Setup(x => x.GetAsync("hermes.pay", "duplicate-key", "commerce-user"))
            .ReturnsAsync(new IdempotencyRecord { Operation = "hermes.pay", Key = "duplicate-key", ActorUserId = "commerce-user" });
        unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transaction.Object);
        users.Setup(x => x.GetByIdAsync(It.IsAny<string>())).ReturnsAsync(new UserDto { IsActive = true, Id = "card-owner" });

        var service = new PaymentProcessorService(
            cards.Object, commerces.Object, consumptions.Object, accounts.Object, transactions.Object,
            unitOfWork.Object, users.Object, email.Object, idempotency.Object);

        var result = await service.ProcessPaymentAsync(10, payment);

        result.Success.Should().BeFalse();
        result.Message.Should().Contain("already been processed");
        cards.Verify(x => x.ChargeWithoutSaveAsync(It.IsAny<int>(), It.IsAny<decimal>()), Times.Never);
        idempotency.Verify(x => x.AddWithoutSaveAsync(It.IsAny<IdempotencyRecord>()), Times.Never);
        transaction.Verify(x => x.RollbackAsync(It.IsAny<CancellationToken>()), Times.Once);
        transactions.Verify(x => x.AddWithoutSaveAsync(It.IsAny<Transaction>()), Times.Never);
    }
}
