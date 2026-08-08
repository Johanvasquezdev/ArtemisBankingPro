using System;
using System.Threading.Tasks;
using ABP.Core.Application.DTOs.Cashier;
using ABP.Core.Domain.Interfaces;
using ABP.Core.Application.Interfaces.Services;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using AutoMapper;
using FluentAssertions;
using Moq;
using Xunit;
using System.Collections.Generic;
using Microsoft.AspNetCore.Http;
using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Interfaces.IServices;

namespace ABP.Unit.Tests.Services
{
    public class TransactionServiceTests
    {
        private readonly Mock<ITransactionRepository> _mockTransactionRepo;
        private readonly Mock<ISavingsAccountRepository> _mockSavingsRepo;
        private readonly Mock<IMapper> _mockMapper;
        private readonly Mock<IUserReadOnlyService> _mockUserReadOnlyService;
        private readonly Mock<IEmailServices> _mockEmailService;
        private readonly Mock<ICreditCardRepository> _mockCreditCardRepo;
        private readonly Mock<ILoanRepository> _mockLoanRepo;
        private readonly Mock<ILoanInstallmentRepository> _mockInstallmentRepo;
        private readonly TransactionService _service;

        public TransactionServiceTests()
        {
            _mockTransactionRepo = new Mock<ITransactionRepository>();
            _mockSavingsRepo = new Mock<ISavingsAccountRepository>();
            _mockMapper = new Mock<IMapper>();
            _mockUserReadOnlyService = new Mock<IUserReadOnlyService>();
            _mockEmailService = new Mock<IEmailServices>();
            _mockCreditCardRepo = new Mock<ICreditCardRepository>();
            _mockLoanRepo = new Mock<ILoanRepository>();
            _mockInstallmentRepo = new Mock<ILoanInstallmentRepository>();

            _service = new TransactionService(
                _mockTransactionRepo.Object, 
                _mockSavingsRepo.Object, 
                _mockMapper.Object, 
                _mockUserReadOnlyService.Object,
                _mockEmailService.Object,
                _mockCreditCardRepo.Object,
                _mockLoanRepo.Object,
                _mockInstallmentRepo.Object
            );
        }

        [Fact]
        public async Task DepositAsync_ShouldThrowException_WhenAccountDoesNotExist()
        {
            // Arrange
            var request = new CashierDepositDto { AccountNumber = "INVALID", Amount = 100 };
            _mockSavingsRepo.Setup(x => x.GetByAccountNumberAsync(request.AccountNumber))
                .ReturnsAsync((SavingsAccount?)null);

            // Act
            Func<Task> act = async () => await _service.DepositAsync(request);

            // Assert
            await act.Should().ThrowAsync<Exception>().WithMessage("The destination account does not exist.");
        }

        [Fact]
        public async Task DepositAsync_ShouldSucceed_WhenAccountExists()
        {
            // Arrange
            var request = new CashierDepositDto { AccountNumber = "ACC-1", Amount = 100 };
            var account = new SavingsAccount { Id = 1, AccountNumber = "ACC-1", Balance = 500, Status = AccountStatus.Active };
            
            _mockSavingsRepo.Setup(x => x.GetByAccountNumberAsync(request.AccountNumber))
                .ReturnsAsync(account);
            _mockUserReadOnlyService.Setup(x => x.GetByIdAsync(It.IsAny<string>()))
                .ReturnsAsync((UserDto?)null);

            // Act
            await _service.DepositAsync(request);

            // Assert
            account.Balance.Should().Be(600); // 500 + 100
            
            _mockSavingsRepo.Verify(x => x.UpdateAsync(account), Times.Once);
            _mockTransactionRepo.Verify(x => x.AddAsync(It.IsAny<Transaction>()), Times.Once);
        }

        [Fact]
        public async Task WithdrawAsync_ShouldThrowException_WhenInsufficientFunds()
        {
            // Arrange
            var request = new CashierWithdrawalDto { AccountNumber = "ACC-1", Amount = 1000 };
            var account = new SavingsAccount { Id = 1, AccountNumber = "ACC-1", Balance = 500, Status = AccountStatus.Active };

            _mockSavingsRepo.Setup(x => x.GetByAccountNumberAsync(request.AccountNumber))
                .ReturnsAsync(account);

            // Act
            Func<Task> act = async () => await _service.WithdrawAsync(request);

            // Assert
            await act.Should().ThrowAsync<InvalidOperationException>().WithMessage("Insufficient funds. Current balance: $500.00");
        }
    }
}
