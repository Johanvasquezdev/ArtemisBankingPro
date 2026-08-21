using System.Security.Claims;
using ABP.Core.Application.DTOs.SavingsAccount;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.Features.Admin.Commands;
using ABP.Core.Application.Features.Admin.Queries;
using ABP.Core.Domain.Enums;
using ArtemisBankingPro.Areas.Admin.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using MediatR;
using Xunit;

namespace ABP.Unit.Tests.Controllers.Presentation
{
    public class SavingsAccountManagementControllerTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly SavingsAccountManagementController _controller;

        public SavingsAccountManagementControllerTests()
        {
            _mockMediator = new Mock<IMediator>();

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("uid", "admin-1") }))
            };

            _controller = new SavingsAccountManagementController(_mockMediator.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };
        }

        [Fact]
        public async Task Assign_ShouldRedirectWithError_WhenClientHasNoPrimaryAccount()
        {
            // Arrange
            _mockMediator.Setup(m => m.Send(It.IsAny<GetPrimarySavingsAccountQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((SavingsAccountDto?)null);

            // Act
            var result = await _controller.Assign("client1");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["ErrorMessage"].Should().NotBeNull();
        }

        [Fact]
        public async Task Cancel_ShouldRedirectWithError_WhenAccountIsPrimary()
        {
            // Arrange
            var account = new SavingsAccountDto { AccountNumber = "123456789", Type = AccountType.Primary };
            _mockMediator.Setup(m => m.Send(It.IsAny<GetAdminSavingsAccountQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(account);

            // Act
            var result = await _controller.Cancel("123456789");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["ErrorMessage"].Should().Be("Las cuentas principales no pueden ser canceladas.");
        }

        [Fact]
        public async Task Details_ShouldReturnNotFound_WhenAccountDoesNotExist()
        {
            _mockMediator.Setup(m => m.Send(It.IsAny<GetAdminSavingsAccountTransactionsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AdminSavingsAccountTransactionsResult?)null);

            var result = await _controller.Details("999999999");

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task CancelConfirmed_ShouldRedirectToIndex_AndCallCancelAsync()
        {
            // Act
            _mockMediator.Setup(m => m.Send(It.IsAny<CancelSavingsAccountCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(MediatR.Unit.Value);
            var result = await _controller.CancelConfirmed("123456789");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _mockMediator.Verify(m => m.Send(It.IsAny<CancelSavingsAccountCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }
    }
}
