using ABP.Core.Application.Interfaces.Services;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Domain.Interfaces;
using FluentAssertions;
using Moq;
using Xunit;

namespace ABP.Unit.Tests.Services;

public sealed class SavingsAccountServiceTests
{
    private readonly Mock<ISavingsAccountRepository> _accounts = new();
    private readonly Mock<AutoMapper.IMapper> _mapper = new();
    private readonly Mock<IUserReadOnlyService> _users = new();
    private readonly Mock<ITransactionRepository> _transactions = new();
    private readonly Mock<IUnitOfWork> _unitOfWork = new();
    private readonly Mock<IUnitOfWorkTransaction> _transaction = new();

    private SavingsAccountService CreateService() => new(
        _accounts.Object, _mapper.Object, _users.Object, _transactions.Object, _unitOfWork.Object);

    [Fact]
    public async Task Deposit_ShouldUpdateActiveAccountAndSaveOnce()
    {
        var account = new SavingsAccount { Id = 1, AccountNumber = "123456789", Balance = 100, Status = AccountStatus.Active };
        _accounts.Setup(x => x.GetByAccountNumberAsync(account.AccountNumber)).ReturnsAsync(account);

        var result = await CreateService().DepositAsync(account.AccountNumber, 25);

        result.Should().BeTrue();
        account.Balance.Should().Be(125);
        _accounts.Verify(x => x.UpdateWithoutSaveAsync(account), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Theory]
    [InlineData(0)]
    [InlineData(-10)]
    public async Task Deposit_ShouldRejectNonPositiveAmount(decimal amount)
    {
        var result = await CreateService().DepositAsync("123456789", amount);

        result.Should().BeFalse();
        _accounts.Verify(x => x.GetByAccountNumberAsync(It.IsAny<string>()), Times.Never);
    }

    [Fact]
    public async Task Withdraw_ShouldRejectInsufficientBalanceWithoutWriting()
    {
        var account = new SavingsAccount { AccountNumber = "123456789", Balance = 100, Status = AccountStatus.Active };
        _accounts.Setup(x => x.GetByAccountNumberAsync(account.AccountNumber)).ReturnsAsync(account);

        var result = await CreateService().WithdrawAsync(account.AccountNumber, 101);

        result.Should().BeFalse();
        account.Balance.Should().Be(100);
        _accounts.Verify(x => x.UpdateWithoutSaveAsync(It.IsAny<SavingsAccount>()), Times.Never);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }

    [Fact]
    public async Task Transfer_ShouldMoveFundsAndCommitAsOneUnit()
    {
        var source = new SavingsAccount { AccountNumber = "123456789", Balance = 500, Status = AccountStatus.Active };
        var destination = new SavingsAccount { AccountNumber = "987654321", Balance = 20, Status = AccountStatus.Active };
        _accounts.Setup(x => x.GetByAccountNumberAsync(source.AccountNumber)).ReturnsAsync(source);
        _accounts.Setup(x => x.GetByAccountNumberAsync(destination.AccountNumber)).ReturnsAsync(destination);
        _unitOfWork.Setup(x => x.BeginTransactionAsync(It.IsAny<CancellationToken>())).ReturnsAsync(_transaction.Object);

        var result = await CreateService().TransferAsync(source.AccountNumber, destination.AccountNumber, 125);

        result.Should().BeTrue();
        source.Balance.Should().Be(375);
        destination.Balance.Should().Be(145);
        _accounts.Verify(x => x.UpdateWithoutSaveAsync(source), Times.Once);
        _accounts.Verify(x => x.UpdateWithoutSaveAsync(destination), Times.Once);
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
        _transaction.Verify(x => x.CommitAsync(It.IsAny<CancellationToken>()), Times.Once);
    }

    [Fact]
    public async Task ChangeStatus_ShouldReturnFalseWhenAccountDoesNotExist()
    {
        _accounts.Setup(x => x.GetByIdAsync(9)).ReturnsAsync((SavingsAccount?)null);

        var result = await CreateService().ChangeStatusAsync(9, AccountStatus.Closed);

        result.Should().BeFalse();
        _unitOfWork.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Never);
    }
}
