using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.DTOs.Cashier;
using ABP.Core.Application.DTOs.Payment;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Features.Account.Commands;
using ABP.Core.Application.Features.Cashier.Commands;
using ABP.Core.Application.Features.Cashier.Queries;
using ABP.Core.Application.Features.Commerce.Commands;
using ABP.Core.Application.Features.Commerce.Queries;
using ABP.Core.Application.Features.Functions.Commands;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using FluentAssertions;
using MediatR;
using Moq;
using Xunit;

namespace ABP.Unit.Tests.Features;

public sealed class AccountCommandTests
{
    [Fact]
    public async Task RegisterHandler_ShouldForwardWebChannel()
    {
        var users = new Mock<IUserService>();
        users.Setup(x => x.RegisterAsync("Johan", "Vasquez", "40200000000", "johan", "johan@test.local", "Password1!", "Cliente", "admin", 100, AccountEmailChannel.Web))
            .ReturnsAsync(true);

        var command = new RegisterAccountCommand("Johan", "Vasquez", "40200000000", "johan", "johan@test.local", "Password1!", "Cliente", "admin", 100);

        var result = await new RegisterAccountCommandHandler(users.Object).Handle(command, CancellationToken.None);

        result.Should().BeTrue();
        users.Verify(x => x.RegisterAsync("Johan", "Vasquez", "40200000000", "johan", "johan@test.local", "Password1!", "Cliente", "admin", 100, AccountEmailChannel.Web), Times.Once);
    }

    [Fact]
    public async Task LoginHandler_ShouldGenerateJwtOnlyAfterSuccessfulAuthentication()
    {
        var users = new Mock<IUserService>();
        var jwt = new Mock<IJwtService>();
        users.Setup(x => x.AuthenticateAsync("johan", "Password1!"))
            .ReturnsAsync(new AuthenticationResult
            {
                Success = true,
                UserId = "user-1",
                UserName = "johan",
                Email = "johan@test.local",
                Role = UserRole.Client,
                CommerceId = 0
            });
        jwt.Setup(x => x.GenerateTokenAsync("user-1", "johan", "johan@test.local", It.Is<IEnumerable<string>>(roles => roles.Single() == UserRole.Client.ToString()), 0))
            .ReturnsAsync("jwt-token");

        var result = await new LoginCommandHandler(users.Object, jwt.Object)
            .Handle(new LoginCommand("johan", "Password1!"), CancellationToken.None);

        result.JwtToken.Should().Be("jwt-token");
        jwt.VerifyAll();
    }

    [Fact]
    public async Task LoginHandler_ShouldNotGenerateJwtWhenAuthenticationFails()
    {
        var users = new Mock<IUserService>();
        var jwt = new Mock<IJwtService>();
        var expected = new AuthenticationResult { Success = false, Error = "Credenciales inválidas" };
        users.Setup(x => x.AuthenticateAsync("johan", "bad")).ReturnsAsync(expected);

        var result = await new LoginCommandHandler(users.Object, jwt.Object)
            .Handle(new LoginCommand("johan", "bad"), CancellationToken.None);

        result.Should().BeSameAs(expected);
        jwt.Verify(x => x.GenerateTokenAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>(), It.IsAny<IEnumerable<string>>(), It.IsAny<int?>()), Times.Never);
    }

    [Fact]
    public async Task LogoutHandler_ShouldLogoutAndReturnUnit()
    {
        var users = new Mock<IUserService>();

        var result = await new LogoutCommandHandler(users.Object).Handle(new LogoutCommand(), CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        users.Verify(x => x.LogoutAsync(), Times.Once);
    }

    [Fact]
    public async Task ActivateHandler_ShouldForwardToken()
    {
        var users = new Mock<IUserService>();
        users.Setup(x => x.ActivateAccountAsync("token")).ReturnsAsync(true);

        var result = await new ActivateAccountCommandHandler(users.Object)
            .Handle(new ActivateAccountCommand("token"), CancellationToken.None);

        result.Should().BeTrue();
        users.Verify(x => x.ActivateAccountAsync("token"), Times.Once);
    }

    [Fact]
    public async Task ResetTokenHandler_ShouldForwardEmailChannel()
    {
        var users = new Mock<IUserService>();
        users.Setup(x => x.GeneratePasswordResetTokenAsync("johan", AccountEmailChannel.Api)).ReturnsAsync(true);

        var result = await new GeneratePasswordResetTokenCommandHandler(users.Object)
            .Handle(new GeneratePasswordResetTokenCommand("johan", AccountEmailChannel.Api), CancellationToken.None);

        result.Should().BeTrue();
        users.Verify(x => x.GeneratePasswordResetTokenAsync("johan", AccountEmailChannel.Api), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordHandler_ShouldForwardCredentials()
    {
        var users = new Mock<IUserService>();
        users.Setup(x => x.ResetPasswordAsync("johan", "token", "Password1!"))
            .ReturnsAsync(true);

        var result = await new ResetPasswordCommandHandler(users.Object)
            .Handle(new ResetPasswordCommand("johan", "token", "Password1!"), CancellationToken.None);

        result.Should().BeTrue();
        users.Verify(x => x.ResetPasswordAsync("johan", "token", "Password1!"), Times.Once);
    }

    [Fact]
    public async Task ResetPasswordByUserIdHandler_ShouldReturnFalseForUnknownUser()
    {
        var users = new Mock<IUserService>();
        var readOnly = new Mock<IUserReadOnlyService>();
        readOnly.Setup(x => x.GetByIdAsync("missing")).ReturnsAsync((UserDto?)null);

        var result = await new ResetPasswordByUserIdCommandHandler(users.Object, readOnly.Object)
            .Handle(new ResetPasswordByUserIdCommand("missing", "token", "Password1!"), CancellationToken.None);

        result.Should().BeFalse();
        users.Verify(x => x.ResetPasswordAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}

public sealed class CashierCommandTests
{
    [Fact]
    public async Task CashierCommands_ShouldDelegateEveryOperation()
    {
        var service = new Mock<ICashierTransactionService>();
        var deposit = new CashierDepositDto { AccountNumber = "100000001", Amount = 100, PerformedByUserId = "cashier", IdempotencyKey = "d" };
        var withdrawal = new CashierWithdrawalDto { AccountNumber = "100000001", Amount = 50, PerformedByUserId = "cashier", IdempotencyKey = "w" };
        var card = new CashierPayCreditCardDto { SourceAccountNumber = "100000001", CardNumber = "4111111111111111", Amount = 20, PerformedByUserId = "cashier", IdempotencyKey = "c" };
        var loan = new CashierPayLoanDto { SourceAccountNumber = "100000001", LoanNumber = "300000001", Amount = 20, PerformedByUserId = "cashier", IdempotencyKey = "l" };
        var transfer = new CashierTransferDto { SourceAccountNumber = "100000001", DestinationAccountNumber = "100000002", Amount = 20, PerformedByUserId = "cashier", IdempotencyKey = "t" };

        var handlers = new Func<Task<MediatR.Unit>>[]
        {
            () => new DepositCashierCommandHandler(service.Object).Handle(new(deposit), CancellationToken.None),
            () => new WithdrawCashierCommandHandler(service.Object).Handle(new(withdrawal), CancellationToken.None),
            () => new PayCashierCreditCardCommandHandler(service.Object).Handle(new(card), CancellationToken.None),
            () => new PayCashierLoanCommandHandler(service.Object).Handle(new(loan), CancellationToken.None),
            () => new TransferCashierCommandHandler(service.Object).Handle(new(transfer), CancellationToken.None)
        };

        foreach (var handler in handlers)
            (await handler()).Should().Be(MediatR.Unit.Value);

        service.Verify(x => x.DepositAsync(deposit), Times.Once);
        service.Verify(x => x.WithdrawAsync(withdrawal), Times.Once);
        service.Verify(x => x.CashierPayCreditCardAsync(card), Times.Once);
        service.Verify(x => x.CashierPayLoanAsync(loan), Times.Once);
        service.Verify(x => x.CashierTransferAsync(transfer), Times.Once);
    }

    [Fact]
    public void CashierValidators_ShouldRequireIdempotencyAndPositiveAmounts()
    {
        var deposit = new DepositCashierCommandValidator().Validate(new DepositCashierCommand(new CashierDepositDto()));
        var withdrawal = new WithdrawCashierCommandValidator().Validate(new WithdrawCashierCommand(new CashierWithdrawalDto()));
        var card = new PayCashierCreditCardCommandValidator().Validate(new PayCashierCreditCardCommand(new CashierPayCreditCardDto()));
        var loan = new PayCashierLoanCommandValidator().Validate(new PayCashierLoanCommand(new CashierPayLoanDto()));
        var transfer = new TransferCashierCommandValidator().Validate(new TransferCashierCommand(new CashierTransferDto()));

        deposit.IsValid.Should().BeFalse();
        withdrawal.IsValid.Should().BeFalse();
        card.IsValid.Should().BeFalse();
        loan.IsValid.Should().BeFalse();
        transfer.IsValid.Should().BeFalse();
        deposit.Errors.Should().Contain(error => error.PropertyName == "Dto.IdempotencyKey");
    }
}

public sealed class CommerceFeatureTests
{
    [Fact]
    public async Task ProcessPaymentHandler_ShouldDelegateCommerceIdAndPayment()
    {
        var service = new Mock<IPaymentProcessorService>();
        var payment = new ProcessPaymentDto
        {
            CardNumber = "4111111111111111",
            MonthExpirationCard = "12",
            YearExpirationCard = "30",
            CVC = "123",
            TransactionAmount = 100,
            IdempotencyKey = "payment-1"
        };
        var expected = new PaymentResultDto { Success = true, TransactionId = 1 };
        service.Setup(x => x.ProcessPaymentAsync(10, payment)).ReturnsAsync(expected);

        var result = await new ProcessCommercePaymentCommandHandler(service.Object)
            .Handle(new ProcessCommercePaymentCommand(10, payment), CancellationToken.None);

        result.Should().BeSameAs(expected);
        service.Verify(x => x.ProcessPaymentAsync(10, payment), Times.Once);
    }

    [Fact]
    public async Task CommerceTransactionsQuery_ShouldDelegateCommerceId()
    {
        var service = new Mock<IPaymentProcessorService>();
        var expected = new ABP.Core.Application.DTOs.PaginatedResult<PaymentTransactionDto> { Items = new[] { new PaymentTransactionDto { Id = 1, Amount = 100 } } };
        service.Setup(x => x.GetCommerceTransactionsAsync(10, 1, 10)).ReturnsAsync(expected);

        var result = await new GetCommerceTransactionsQueryHandler(service.Object)
            .Handle(new GetCommerceTransactionsQuery(10, 1, 10), CancellationToken.None);

        result.Should().BeSameAs(expected);
        service.Verify(x => x.GetCommerceTransactionsAsync(10, 1, 10), Times.Once);
    }

    [Fact]
    public void PaymentValidator_ShouldRequireIdempotencyKeyAndAmount()
    {
        var result = new ProcessCommercePaymentCommandValidator()
            .Validate(new ProcessCommercePaymentCommand(0, new ProcessPaymentDto()));

        result.IsValid.Should().BeFalse();
        result.Errors.Should().Contain(error => error.PropertyName == "CommerceId");
        result.Errors.Should().Contain(error => error.PropertyName == "Payment.IdempotencyKey");
        result.Errors.Should().Contain(error => error.PropertyName == "Payment.TransactionAmount");
    }
}

public sealed class FunctionCommandTests
{
    [Fact]
    public async Task BillingCycleHandler_ShouldCountActiveCards()
    {
        var cards = new Mock<ABP.Core.Domain.Interfaces.ICreditCardRepository>();
        cards.Setup(x => x.GetAllAsync()).ReturnsAsync(new[]
        {
            new CreditCard { Status = CardStatus.Active },
            new CreditCard { Status = CardStatus.Cancelled },
            new CreditCard { Status = CardStatus.Active }
        });

        var result = await new RunCreditCardBillingCycleCommandHandler(cards.Object)
            .Handle(new RunCreditCardBillingCycleCommand(), CancellationToken.None);

        result.Should().Be(2);
    }

    [Fact]
    public async Task DailyIndicatorsHandler_ShouldReturnTransactionAndPaymentCounts()
    {
        var transactions = new Mock<ITransactionQueryService>();
        transactions.Setup(x => x.GetTodayTransactionsCountAsync()).ReturnsAsync(7);
        transactions.Setup(x => x.GetTodayPaymentsCountAsync()).ReturnsAsync(3);

        var result = await new GenerateDailyIndicatorsCommandHandler(transactions.Object)
            .Handle(new GenerateDailyIndicatorsCommand(), CancellationToken.None);

        result.Transactions.Should().Be(7);
        result.Payments.Should().Be(3);
    }

    [Fact]
    public async Task EmailCommand_ShouldDeserializeAndSendMessage()
    {
        var email = new Mock<IEmailServices>();
        var message = "{\"to\":\"client@test.local\",\"subject\":\"Aviso\",\"body\":\"Contenido\"}";

        var result = await new ProcessEmailMessageCommandHandler(email.Object)
            .Handle(new ProcessEmailMessageCommand(message), CancellationToken.None);

        result.Should().BeTrue();
        email.Verify(x => x.SendAsync("client@test.local", "Aviso", "Contenido"), Times.Once);
    }

    [Fact]
    public async Task EmailCommand_ShouldRejectInvalidMessage()
    {
        var email = new Mock<IEmailServices>();

        var result = await new ProcessEmailMessageCommandHandler(email.Object)
            .Handle(new ProcessEmailMessageCommand("{}"), CancellationToken.None);

        result.Should().BeFalse();
        email.Verify(x => x.SendAsync(It.IsAny<string>(), It.IsAny<string>(), It.IsAny<string>()), Times.Never);
    }
}
