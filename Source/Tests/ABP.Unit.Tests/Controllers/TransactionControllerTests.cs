using System;
using System.Threading.Tasks;
using ABP.API.Controllers.v1;
using ABP.Core.Application.DTOs.Cashier;
using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.Interfaces.IServices;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ABP.Unit.Tests.Controllers
{
    public class TransactionControllerTests
    {
        private readonly Mock<ITransactionService> _mockTransactionService;
        private readonly Mock<IDashboardService> _mockDashboardService;
        private readonly TransactionController _controller;

        public TransactionControllerTests()
        {
            _mockTransactionService = new Mock<ITransactionService>();
            _mockDashboardService = new Mock<IDashboardService>();
            
            _controller = new TransactionController(
                _mockTransactionService.Object,
                _mockDashboardService.Object
            );
        }

        [Fact]
        public async Task Deposit_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            var request = new CashierDepositDto { AccountNumber = "ACC-1", Amount = 100 };
            _mockTransactionService.Setup(s => s.DepositAsync(request))
                .Returns(Task.CompletedTask);

            // Act
            var result = await _controller.Deposit(request);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.StatusCode.Should().Be(200);
        }

        [Fact]
        public async Task Withdraw_ShouldReturnBadRequest_WhenServiceThrowsException()
        {
            // Arrange
            var request = new CashierWithdrawalDto { AccountNumber = "ACC-1", Amount = 1000 };
            _mockTransactionService.Setup(s => s.WithdrawAsync(request))
                .ThrowsAsync(new InvalidOperationException("Fondos insuficientes."));

            // Act
            var result = await _controller.Withdraw(request);

            // Assert
            var badRequest = result.Should().BeOfType<BadRequestObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(400);
        }
    }
}
