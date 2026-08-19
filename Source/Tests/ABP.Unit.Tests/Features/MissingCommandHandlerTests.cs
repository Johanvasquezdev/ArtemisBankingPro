using ABP.Core.Application.DTOs.Common;
using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.DTOs.Commerce;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.DTOs.SavingsAccount;
using ABP.Core.Application.DTOs.Transaction;
using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Features.Admin.Commands;
using ABP.Core.Application.Features.Client.Commands;
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

public sealed class MissingClientCommandHandlerTests
{
    [Fact]
    public async Task PaymentAndTransferHandlers_ShouldDelegateTheExactDto()
    {
        var service = new Mock<IClientTransactionService>();
        var beneficiary = new PayBeneficiaryDto { ClientId = "client-1", BeneficiaryId = 3, SourceAccountNumber = "123456789", Amount = 100, IdempotencyKey = "b-1" };
        var card = new PayCreditCardDto { ClientId = "client-1", SourceAccountNumber = "123456789", CreditCardNumber = "1234567890123456", Amount = 200, IdempotencyKey = "c-1" };
        var loan = new PayLoanDto { ClientId = "client-1", SourceAccountNumber = "123456789", LoanNumber = "LN-1", Amount = 300, IdempotencyKey = "l-1" };
        var transfer = new TransferOwnAccountsDto { ClientId = "client-1", SourceAccountNumber = "123456789", DestinationAccountNumber = "987654321", Amount = 400, IdempotencyKey = "t-1" };
        service.Setup(x => x.PayBeneficiaryAsync(beneficiary)).ReturnsAsync(CommandResult.Success());
        service.Setup(x => x.PayCreditCardAsync(card)).ReturnsAsync(CommandResult.Success());
        service.Setup(x => x.PayLoanAsync(loan)).ReturnsAsync(CommandResult.Success());
        service.Setup(x => x.TransferOwnAccountsAsync(transfer)).ReturnsAsync(CommandResult.Success());

        (await new PayBeneficiaryCommandHandler(service.Object).Handle(new PayBeneficiaryCommand(beneficiary), CancellationToken.None)).Succeeded.Should().BeTrue();
        (await new PayCreditCardCommandHandler(service.Object).Handle(new PayCreditCardCommand(card), CancellationToken.None)).Succeeded.Should().BeTrue();
        (await new PayLoanCommandHandler(service.Object).Handle(new PayLoanCommand(loan), CancellationToken.None)).Succeeded.Should().BeTrue();
        (await new TransferOwnAccountsCommandHandler(service.Object).Handle(new TransferOwnAccountsCommand(transfer), CancellationToken.None)).Succeeded.Should().BeTrue();

        service.Verify(x => x.PayBeneficiaryAsync(beneficiary), Times.Once);
        service.Verify(x => x.PayCreditCardAsync(card), Times.Once);
        service.Verify(x => x.PayLoanAsync(loan), Times.Once);
        service.Verify(x => x.TransferOwnAccountsAsync(transfer), Times.Once);
    }

    [Fact]
    public async Task DeleteBeneficiaryHandler_ShouldDelegateOwnerScope()
    {
        var service = new Mock<IBeneficiaryService>();
        var result = await new DeleteBeneficiaryCommandHandler(service.Object)
            .Handle(new DeleteBeneficiaryCommand(8, "client-1"), CancellationToken.None);

        result.Should().Be(MediatR.Unit.Value);
        service.Verify(x => x.DeleteAsync(8, "client-1"), Times.Once);
    }

    [Fact]
    public void PaymentValidators_ShouldRejectMissingBusinessIdentifiers()
    {
        new PayBeneficiaryCommandValidator().Validate(new PayBeneficiaryCommand(new PayBeneficiaryDto { Amount = 1 })).IsValid.Should().BeFalse();
        new PayCreditCardCommandValidator().Validate(new PayCreditCardCommand(new PayCreditCardDto { Amount = 1 })).IsValid.Should().BeFalse();
        new PayLoanCommandValidator().Validate(new PayLoanCommand(new PayLoanDto { Amount = 1 })).IsValid.Should().BeFalse();
        new DeleteBeneficiaryCommandValidator().Validate(new DeleteBeneficiaryCommand(0, "")).IsValid.Should().BeFalse();
    }
}

public sealed class MissingAdminCommandHandlerTests
{
    [Fact]
    public async Task CommerceCardSavingsAndLoanCommands_ShouldDelegate()
    {
        var commerce = new Mock<ICommerceService>();
        var cards = new Mock<ICreditCardService>();
        var accounts = new Mock<ISavingsAccountService>();
        var loans = new Mock<ILoanService>();
        var userServiceMock = new Mock<IUserService>();
        var commerceDto = new CommerceDto
        {
            Id = 4,
            Name = "Updated",
            Description = "test en actualizar comercio comando",
            Logo = "logo.png",
            Rnc = "123456789",
            PhoneNumber = "1234567890",
            Email = "updated@test.local"
        };
        var cardDto = new AssignCreditCardDto { ClientId = "client-1", CreditLimit = 1000 };
        var accountDto = new AssignSavingsAccountDto { ClientId = "client-1", AdminId = "admin-1", InitialBalance = 10 };

        commerce.Setup(x => x.GetByIdAsync(4)).ReturnsAsync(commerceDto);
        loans.Setup(x => x.GetByIdAsync(11)).ReturnsAsync(new LoanDto { Id = 11 });

        await new UpdateCommerceCommandHandler(commerce.Object).Handle(
            new UpdateCommerceCommand(
                commerceDto.Id,
                commerceDto.Name,
                commerceDto.Description,
                commerceDto.Logo,
                commerceDto.Email,
                commerceDto.PhoneNumber,
                commerceDto.Rnc
            ),
            CancellationToken.None
        );

        await new ChangeCommerceStatusCommandHandler(commerce.Object, userServiceMock.Object).Handle(new ChangeCommerceStatusCommand(4, false), CancellationToken.None);
        await new UpdateCreditCardLimitCommandHandler(cards.Object).Handle(new UpdateCreditCardLimitCommand(9, 2000), CancellationToken.None);
        await new CancelCreditCardCommandHandler(cards.Object).Handle(new CancelCreditCardCommand(9), CancellationToken.None);

        accounts.Setup(x => x.GetPrimaryAccountByClientIdAsync("client-1"))
            .ReturnsAsync(new SavingsAccountDto { AccountNumber = "123456789", Status = AccountStatus.Active });
        await new AssignSecondarySavingsAccountCommandHandler(accounts.Object)
            .Handle(new AssignSecondarySavingsAccountCommand(accountDto), CancellationToken.None);
        await new CancelSavingsAccountCommandHandler(accounts.Object)
            .Handle(new CancelSavingsAccountCommand("123456789"), CancellationToken.None);
        await new UpdateLoanRateCommandHandler(loans.Object)
            .Handle(new UpdateLoanRateCommand(11, 8.5m), CancellationToken.None);

        commerce.Verify(x => x.UpdateAsync(It.Is<CommerceDto>(d => d.Id == commerceDto.Id && d.Name == commerceDto.Name)), Times.Once);
        commerce.Verify(x => x.ChangeStatusAsync(4, false), Times.Once);
        cards.Verify(x => x.UpdateLimitAsync(9, 2000), Times.Once);
        cards.Verify(x => x.CancelAsync(9), Times.Once);
        accounts.Verify(x => x.AssignSecondaryAsync(accountDto), Times.Once);
        accounts.Verify(x => x.CancelAsync("123456789"), Times.Once);
        loans.Verify(x => x.UpdateInterestRateAsync(11, 8.5m), Times.Once);
    }

    [Fact]
    public async Task AssignSecondarySavings_ShouldRejectClientWithoutPrimaryAccount()
    {
        var accounts = new Mock<ISavingsAccountService>();
        accounts.Setup(x => x.GetPrimaryAccountByClientIdAsync("client-1")).ReturnsAsync((SavingsAccountDto?)null);

        var act = () => new AssignSecondarySavingsAccountCommandHandler(accounts.Object)
            .Handle(new AssignSecondarySavingsAccountCommand(new AssignSavingsAccountDto
            {
                ClientId = "client-1", AdminId = "admin-1", InitialBalance = 0
            }), CancellationToken.None);

        await act.Should().ThrowAsync<InvalidOperationException>()
            .WithMessage("*cuenta principal activa*");
        accounts.Verify(x => x.AssignSecondaryAsync(It.IsAny<AssignSavingsAccountDto>()), Times.Never);
    }

    [Fact]
    public async Task UserCommands_ShouldPassEmailChannelAndStatus()
    {
        var users = new Mock<IUserService>();
        users.Setup(x => x.RegisterAsync("Johan", "Vasquez", "40200000000", "johan", "johan@test.local", "Password1!", "Cliente", "admin-1", 500, AccountEmailChannel.Api))
            .ReturnsAsync(true);
        users.Setup(x => x.RegisterCommerceUserAsync("Ana", "Diaz", "40200000001", "ana", "ana@test.local", "Password1!", 4, AccountEmailChannel.Api))
            .ReturnsAsync(true);
        users.Setup(x => x.UpdateAsync(It.IsAny<UpdateUserDto>())).ReturnsAsync(true);
        users.Setup(x => x.ChangeStatusAsync("admin-1", "user-1", true)).ReturnsAsync(true);

        (await new CreateUserCommandHandler(users.Object).Handle(new CreateUserCommand(
            "Johan", "Vasquez", "40200000000", "johan", "johan@test.local", "Password1!", "Cliente", "admin-1", 500, AccountEmailChannel.Api), CancellationToken.None)).Success.Should().BeTrue();
        (await new CreateCommerceUserCommandHandler(users.Object).Handle(new CreateCommerceUserCommand(
            "Ana", "Diaz", "40200000001", "ana", "ana@test.local", "Password1!", 4, AccountEmailChannel.Api), CancellationToken.None)).Should().BeTrue();
        var update = new UpdateUserDto { Id = "user-1", FirstName = "Updated" };
        (await new UpdateUserCommandHandler(users.Object).Handle(new UpdateUserCommand(update), CancellationToken.None)).Should().BeTrue();
            (await new ChangeUserStatusCommandHandler(users.Object).Handle(new ChangeUserStatusCommand("admin-1", "user-1", true), CancellationToken.None)).Success.Should().BeTrue();

        users.Verify(x => x.UpdateAsync(update), Times.Once);
        users.Verify(x => x.ChangeStatusAsync("admin-1", "user-1", true), Times.Once);
    }
}

public sealed class FunctionCommandBehaviorTests
{
    [Fact]
    public async Task LoanLateFeeHandler_ShouldMarkAndClearOverdueInstallmentsAtomically()
    {
        var overdue = new LoanInstallment { Id = 1, LoanId = 11, InstallmentAmount = 100, AmountPaid = 0, IsOverdue = false };
        var cleared = new LoanInstallment { Id = 2, LoanId = 11, InstallmentAmount = 100, AmountPaid = 100, IsOverdue = true };
        var installments = new Mock<ILoanInstallmentRepository>();
        var loans = new Mock<ILoanRepository>();
        var unitOfWork = new Mock<IUnitOfWork>();
        var transaction = new Mock<IUnitOfWorkTransaction>();
        installments.Setup(x => x.GetOverdueInstallmentsByLoanIdsAsync(
                It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 11 }))))
            .ReturnsAsync(new[] { overdue });
        installments.Setup(x => x.GetByLoanIdsAsync(It.IsAny<IEnumerable<int>>())).ReturnsAsync(new[] { cleared });
        loans.Setup(x => x.GetAllAsync()).ReturnsAsync(new[]
        {
            new Loan { Id = 10, Status = LoanStatus.Completed },
            new Loan { Id = 11, Status = LoanStatus.Active }
        });
        unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(transaction.Object);
        transaction.Setup(x => x.CommitAsync(It.IsAny<CancellationToken>())).Returns(Task.CompletedTask);

        var result = await new RunLoanLateFeeAndInterestCommandHandler(installments.Object, loans.Object, unitOfWork.Object)
            .Handle(new RunLoanLateFeeAndInterestCommand(), CancellationToken.None);

        result.Should().Be(new LoanOverdueResult(1, 1));
        overdue.IsOverdue.Should().BeTrue();
        cleared.IsOverdue.Should().BeFalse();
        installments.Verify(x => x.GetOverdueInstallmentsByLoanIdsAsync(
            It.Is<IEnumerable<int>>(ids => ids.SequenceEqual(new[] { 11 }))), Times.Once);
        installments.Verify(x => x.UpdateWithoutSaveAsync(overdue), Times.Once);
        installments.Verify(x => x.UpdateWithoutSaveAsync(cleared), Times.Once);
        unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }
}
