using ABP.API.Controllers.v1.Admin;
using ABP.API.DTOs.SavingsAccount;
using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.DTOs.SavingsAccount;
using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace ABP.Unit.Tests.Controllers
{
    public class SavingsAccountApiControllerTests
    {
        private readonly Mock<ISavingsAccountService> _mockAccountService;
        private readonly Mock<IUserReadOnlyService> _mockUserReadOnlyService;
        private readonly SavingsAccountApiController _controller;

        public SavingsAccountApiControllerTests()
        {
            _mockAccountService = new Mock<ISavingsAccountService>();
            _mockUserReadOnlyService = new Mock<IUserReadOnlyService>();

            _controller = new SavingsAccountApiController(_mockAccountService.Object, _mockUserReadOnlyService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("uid", "admin-1")]))
                    }
                }
            };
        }

        [Fact]
        public async Task GetAll_ShouldReturnBadRequest_WhenTypeIsInvalid()
        {
            var result = await _controller.GetAll(1, 20, null, "activa", "unknown");
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task AssignSecondary_ShouldReturnBadRequest_WhenInitialBalanceIsNegative()
        {
            var request = new AssignSavingsAccountApiDto("00187654321", -100);
            var result = await _controller.AssignSecondary(request);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task AssignSecondary_ShouldReturnNotFound_WhenNoActiveClientMatchesCedula()
        {
            var request = new AssignSavingsAccountApiDto("00187654321", 100);
            _mockUserReadOnlyService.Setup(s => s.GetActiveClientsAsync(request.CedulaClient))
                .ReturnsAsync(Enumerable.Empty<UserDto>());

            var result = await _controller.AssignSecondary(request);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task AssignSecondary_ShouldReturnBadRequest_WhenClientHasNoPrimaryAccount()
        {
            // Arrange
            var request = new AssignSavingsAccountApiDto("00187654321", 100);
            var client = new UserDto { Id = "client1", Cedula = "00187654321", Role = UserRole.Client };

            _mockUserReadOnlyService.Setup(s => s.GetActiveClientsAsync(request.CedulaClient))
                .ReturnsAsync(new[] { client });
            _mockAccountService.Setup(s => s.GetPrimaryAccountByClientIdAsync("client1"))
                .ReturnsAsync((SavingsAccountDto?)null);

            // Act
            var result = await _controller.AssignSecondary(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task AssignSecondary_ShouldReturnCreated_WhenClientHasActivePrimaryAccount()
        {
            // Arrange
            var request = new AssignSavingsAccountApiDto("00187654321", 100);
            var client = new UserDto { Id = "client1", Cedula = "00187654321", Role = UserRole.Client };
            var primary = new SavingsAccountDto { AccountNumber = "123456789", Type = AccountType.Primary };

            _mockUserReadOnlyService.Setup(s => s.GetActiveClientsAsync(request.CedulaClient))
                .ReturnsAsync(new[] { client });
            _mockAccountService.Setup(s => s.GetPrimaryAccountByClientIdAsync("client1"))
                .ReturnsAsync(primary);

            // Act
            var result = await _controller.AssignSecondary(request);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(201);
            _mockAccountService.Verify(s => s.AssignSecondaryAsync(It.Is<AssignSavingsAccountDto>(
                d => d.ClientId == "client1" && d.AdminId == "admin-1")), Times.Once);
        }

        [Fact]
        public async Task GetTransactions_ShouldReturnNotFound_WhenAccountDoesNotExist()
        {
            _mockAccountService.Setup(s => s.GetByAccountNumberAsync("999999999")).ReturnsAsync((SavingsAccountDto?)null);

            var result = await _controller.GetTransactions("999999999");

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Cancel_ShouldReturnBadRequest_WhenAccountIsPrimary()
        {
            _mockAccountService.Setup(s => s.CancelAsync("123456789"))
                .ThrowsAsync(new InvalidOperationException("The primary account cannot be cancelled."));

            var result = await _controller.Cancel("123456789");

            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
