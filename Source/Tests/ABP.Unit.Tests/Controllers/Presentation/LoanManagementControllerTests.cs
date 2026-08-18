using System.Security.Claims;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.Features.Admin.Queries;
using ABP.Core.Application.Features.Admin.Commands;
using ABP.Core.Application.ViewModels.Loan;
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
    public class LoanManagementControllerTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly LoanManagementController _controller;

        public LoanManagementControllerTests()
        {
            _mockMediator = new Mock<IMediator>();

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("uid", "admin-1") }))
            };

            _controller = new LoanManagementController(_mockMediator.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };
        }

        [Fact]
        public async Task Assign_ShouldReturnViewWithError_WhenClientAlreadyHasActiveLoan()
        {
            // Arrange
            var model = new AssignLoanViewModel { ClientId = "client1", Amount = 1000, AnnualInterestRate = 10, TermInMonths = 12 };
            _mockMediator.Setup(m => m.Send(It.IsAny<AssignLoanCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LoanAssignmentResult(false, false, true, false, 0, 0, null, "El cliente ya tiene un préstamo activo."));

            // Act
            var result = await _controller.Assign(model);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var returnedModel = viewResult.Model.Should().BeOfType<AssignLoanViewModel>().Subject;
            returnedModel.HasError.Should().BeTrue();
        }

        [Fact]
        public async Task Assign_ShouldReturnViewWithRiskWarning_WhenClientIsHighRiskAndNotConfirmed()
        {
            // Arrange
            var model = new AssignLoanViewModel
            {
                ClientId = "client1",
                Amount = 1000,
                AnnualInterestRate = 10,
                TermInMonths = 12,
                RiskConfirmed = false
            };
            _mockMediator.Setup(m => m.Send(It.IsAny<AssignLoanCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LoanAssignmentResult(false, true, false, true, 500m, 800m, null, "Cliente de alto riesgo."));

            // Act
            var result = await _controller.Assign(model);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var returnedModel = viewResult.Model.Should().BeOfType<AssignLoanViewModel>().Subject;
            returnedModel.IsHighRisk.Should().BeTrue();
            _mockMediator.Verify(m => m.Send(It.IsAny<AssignLoanCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Details_ShouldReturnNotFound_WhenLoanDoesNotExist()
        {
            _mockMediator.Setup(m => m.Send(It.IsAny<GetAdminLoanDetailsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AdminLoanDetailsResult?)null);

            var result = await _controller.Details(99);

            result.Should().BeOfType<NotFoundResult>();
        }
    }
}
