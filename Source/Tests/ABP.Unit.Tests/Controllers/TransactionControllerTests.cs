using System;
using System.Threading.Tasks;
using ABP.API.Controllers.v1;
using ABP.Core.Application.DTOs.Cashier;
using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.Features.Cashier.Commands;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ABP.Unit.Tests.Controllers
{
    public class TransactionControllerTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly TransactionController _controller;

        public TransactionControllerTests()
        {
            _mockMediator = new Mock<IMediator>();
            
            _controller = new TransactionController(_mockMediator.Object);
        }

        [Fact]
        public async Task Deposit_ShouldReturnOk_WhenSuccessful()
        {
            // Arrange
            var request = new CashierDepositDto { AccountNumber = "ACC-1", Amount = 100 };
            _mockMediator.Setup(m => m.Send(It.IsAny<DepositCashierCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(MediatR.Unit.Value);

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
            _mockMediator.Setup(m => m.Send(It.IsAny<WithdrawCashierCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Fondos insuficientes."));

            // Act
            var result = await _controller.Withdraw(request);

            // Assert
            var badRequest = result.Should().BeOfType<ObjectResult>().Subject;
            badRequest.StatusCode.Should().Be(400);
        }
    }
}
