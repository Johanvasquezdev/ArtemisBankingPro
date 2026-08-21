using System.Security.Claims;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.Features.Admin.Commands;
using ABP.Core.Application.Features.Admin.Queries;
using ABP.Core.Application.ViewModels.CreditCard;
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
    public class CreditCardManagementControllerTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly CreditCardManagementController _controller;

        public CreditCardManagementControllerTests()
        {
            _mockMediator = new Mock<IMediator>();

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("uid", "admin-1") }))
            };

            _controller = new CreditCardManagementController(_mockMediator.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };
        }

        [Fact]
        public async Task Details_ShouldReturnNotFound_WhenCardDoesNotExist()
        {
            _mockMediator.Setup(m => m.Send(It.IsAny<GetAdminCreditCardDetailsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AdminCreditCardDetailsResult?)null);

            var result = await _controller.Details(99);

            result.Should().BeOfType<NotFoundResult>();
        }

        [Fact]
        public async Task Assign_ShouldRedirectToIndex_WhenSuccessful()
        {
            // Arrange
            var model = new AssignCreditCardViewModel { ClientId = "client1", CreditLimit = 5000 };
            _mockMediator.Setup(m => m.Send(It.IsAny<AssignCreditCardCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreditCardDto { Id = 1, CreditLimit = 5000 });

            // Act
            var result = await _controller.Assign(model);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _mockMediator.Verify(m => m.Send(It.Is<AssignCreditCardCommand>(
                c => c.Card.ClientId == "client1" && c.Card.CreditLimit == 5000), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task EditLimit_ShouldReturnViewWithError_WhenServiceThrows()
        {
            // Arrange
            var model = new EditCreditCardLimitViewModel { CardId = 1, CardNumber = "1234", NewCreditLimit = 100 };
            _mockMediator.Setup(m => m.Send(It.IsAny<UpdateCreditCardLimitCommand>(), It.IsAny<CancellationToken>()))
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
            _mockMediator.Setup(m => m.Send(It.IsAny<CancelCreditCardCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Cannot cancel card. Client owes money."));

            // Act
            var result = await _controller.CancelConfirmed(1);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["ErrorMessage"].Should().NotBeNull();
        }
    }
}
