using System.Security.Claims;
using ABP.Core.Application.DTOs.SavingsAccount;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using ArtemisBankingPro.Areas.Admin.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace ABP.Unit.Tests.Controllers.Presentation
{
    public class SavingsAccountManagementControllerTests
    {
        private readonly Mock<ISavingsAccountService> _mockAccountService;
        private readonly Mock<IUserReadOnlyService> _mockUserReadOnlyService;
        private readonly SavingsAccountManagementController _controller;

        public SavingsAccountManagementControllerTests()
        {
            _mockAccountService = new Mock<ISavingsAccountService>();
            _mockUserReadOnlyService = new Mock<IUserReadOnlyService>();

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("uid", "admin-1") }))
            };

            _controller = new SavingsAccountManagementController(_mockAccountService.Object, _mockUserReadOnlyService.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };
        }

        [Fact]
        public async Task Assign_ShouldRedirectWithError_WhenClientHasNoPrimaryAccount()
        {
            // Arrange
            _mockAccountService.Setup(s => s.GetPrimaryAccountByClientIdAsync("client1")).ReturnsAsync((SavingsAccountDto?)null);

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
            _mockAccountService.Setup(s => s.GetByAccountNumberAsync("123456789")).ReturnsAsync(account);

            // Act
            var result = await _controller.Cancel("123456789");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["ErrorMessage"].Should().Be("Las cuentas principales no pueden ser canceladas.");
        }

        [Fact]
        public async Task Details_ShouldReturnNotFound_WhenAccountDoesNotExist()
        {
            _mockAccountService.Setup(s => s.GetByAccountNumberAsync("999999999")).ReturnsAsync((SavingsAccountDto?)null);

            var result = await _controller.Details("999999999");

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task CancelConfirmed_ShouldRedirectToIndex_AndCallCancelAsync()
        {
            // Act
            var result = await _controller.CancelConfirmed("123456789");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _mockAccountService.Verify(s => s.CancelAsync("123456789"), Times.Once);
        }
    }
}
