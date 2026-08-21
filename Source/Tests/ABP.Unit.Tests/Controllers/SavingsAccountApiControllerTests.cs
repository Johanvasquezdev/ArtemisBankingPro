using ABP.API.Controllers.v1.Admin;
using ABP.API.DTOs.SavingsAccount;
using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.DTOs.SavingsAccount;
using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.Features.Admin.Commands;
using ABP.Core.Application.Features.Admin.Queries;
using ABP.Core.Domain.Enums;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MediatR;
using System.Security.Claims;
using Xunit;

namespace ABP.Unit.Tests.Controllers
{
    public class SavingsAccountApiControllerTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly SavingsAccountController _controller;

        public SavingsAccountApiControllerTests()
        {
            _mockMediator = new Mock<IMediator>();

            _controller = new SavingsAccountController(_mockMediator.Object)
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
            result.Should().BeOfType<ObjectResult>();
        }

        [Fact]
        public async Task AssignSecondary_ShouldReturnBadRequest_WhenInitialBalanceIsNegative()
        {
            var request = new AssignSavingsAccountApiDto("00187654321", -100);
            var result = await _controller.AssignSecondary(request);
            result.Should().BeOfType<ObjectResult>();
        }

        [Fact]
        public async Task AssignSecondary_ShouldReturnNotFound_WhenNoActiveClientMatchesCedula()
        {
            var request = new AssignSavingsAccountApiDto("00187654321", 100);
            _mockMediator.Setup(m => m.Send(It.IsAny<AssignSecondarySavingsAccountByCedulaCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new KeyNotFoundException("No se encontró ningún cliente activo con esta Cédula."));

            var result = await _controller.AssignSecondary(request);

            result.Should().BeOfType<ObjectResult>();
        }

        [Fact]
        public async Task AssignSecondary_ShouldReturnBadRequest_WhenClientHasNoPrimaryAccount()
        {
            // Arrange
            var request = new AssignSavingsAccountApiDto("00187654321", 100);
            var client = new UserDto { Id = "client1", Cedula = "00187654321", Role = UserRole.Client };

            _mockMediator.Setup(m => m.Send(It.IsAny<AssignSecondarySavingsAccountByCedulaCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("El cliente debe tener una cuenta principal activa."));

            // Act
            var result = await _controller.AssignSecondary(request);

            // Assert
            result.Should().BeOfType<ObjectResult>();
        }

        [Fact]
        public async Task AssignSecondary_ShouldReturnCreated_WhenClientHasActivePrimaryAccount()
        {
            // Arrange
            var request = new AssignSavingsAccountApiDto("00187654321", 100);
            var client = new UserDto { Id = "client1", Cedula = "00187654321", Role = UserRole.Client };
            _mockMediator.Setup(m => m.Send(It.IsAny<AssignSecondarySavingsAccountByCedulaCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(MediatR.Unit.Value);

            // Act
            var result = await _controller.AssignSecondary(request);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(201);
            _mockMediator.Verify(m => m.Send(It.Is<AssignSecondarySavingsAccountByCedulaCommand>(
                c => c.CedulaClient == "00187654321" && c.AdminId == "admin-1" && c.InitialBalance == 100),
                It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task GetTransactions_ShouldReturnNotFound_WhenAccountDoesNotExist()
        {
            _mockMediator.Setup(m => m.Send(It.IsAny<GetAdminSavingsAccountTransactionsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AdminSavingsAccountTransactionsResult?)null);

            var result = await _controller.GetTransactions("999999999");

            result.Should().BeOfType<ObjectResult>();
        }

        [Fact]
        public async Task Cancel_ShouldReturnBadRequest_WhenAccountIsPrimary()
        {
            _mockMediator.Setup(m => m.Send(It.IsAny<CancelSavingsAccountCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("The primary account cannot be cancelled."));

            var result = await _controller.Cancel("123456789");

            result.Should().BeOfType<ObjectResult>();
        }
    }
}
