using System.Security.Claims;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.ViewModels.CreditCard;
using ArtemisBankingPro.Areas.Admin.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace ABP.Unit.Tests.Controllers.Presentation
{
    public class CreditCardManagementControllerTests
    {
        private readonly Mock<ICreditCardService> _mockCardService;
        private readonly Mock<ICreditCardConsumptionService> _mockConsumptionService;
        private readonly Mock<IUserReadOnlyService> _mockUserReadOnlyService;
        private readonly CreditCardManagementController _controller;

        public CreditCardManagementControllerTests()
        {
            _mockCardService = new Mock<ICreditCardService>();
            _mockConsumptionService = new Mock<ICreditCardConsumptionService>();
            _mockUserReadOnlyService = new Mock<IUserReadOnlyService>();

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("uid", "admin-1") }))
            };

            _controller = new CreditCardManagementController(
                _mockCardService.Object, _mockConsumptionService.Object, _mockUserReadOnlyService.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };
        }

        [Fact]
        public async Task Details_ShouldReturnNotFound_WhenCardDoesNotExist()
        {
            _mockCardService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((CreditCardDto?)null!);

            var result = await _controller.Details(99);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Assign_ShouldRedirectToIndex_WhenSuccessful()
        {
            // Arrange
            var model = new AssignCreditCardViewModel { ClientId = "client1", CreditLimit = 5000 };

            // Act
            var result = await _controller.Assign(model);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _mockCardService.Verify(s => s.AssignAsync(It.Is<AssignCreditCardDto>(
                d => d.ClientId == "client1" && d.CreditLimit == 5000)), Times.Once);
        }

        [Fact]
        public async Task EditLimit_ShouldReturnViewWithError_WhenServiceThrows()
        {
            // Arrange
            var model = new EditCreditCardLimitViewModel { CardId = 1, CardNumber = "1234", NewCreditLimit = 100 };
            _mockCardService.Setup(s => s.UpdateLimitAsync(1, 100))
                .ThrowsAsync(new InvalidOperationException("The new limit cannot be lower than the current debt."));

            // Act
            var result = await _controller.EditLimit(1, model);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var returnedModel = viewResult.Model.Should().BeOfType<EditCreditCardLimitViewModel>().Subject;
            returnedModel.HasError.Should().BeTrue();
        }

        [Fact]
        public async Task CancelConfirmed_ShouldRedirectToIndex_AndSetTempDataError_WhenCardHasDebt()
        {
            // Arrange
            _mockCardService.Setup(s => s.CancelAsync(1))
                .ThrowsAsync(new InvalidOperationException("Cannot cancel card. Client owes money."));

            // Act
            var result = await _controller.CancelConfirmed(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["ErrorMessage"].Should().NotBeNull();
        }
    }
}
