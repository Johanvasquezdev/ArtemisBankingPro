using System.Security.Claims;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.ViewModels.Loan;
using ArtemisBankingPro.Areas.Admin.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.ViewFeatures;
using Moq;
using Xunit;

namespace ABP.Unit.Tests.Controllers.Presentation
{
    public class LoanManagementControllerTests
    {
        private readonly Mock<ILoanService> _mockLoanService;
        private readonly Mock<ILoanInstallmentService> _mockInstallmentService;
        private readonly LoanManagementController _controller;

        public LoanManagementControllerTests()
        {
            _mockLoanService = new Mock<ILoanService>();
            _mockInstallmentService = new Mock<ILoanInstallmentService>();

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("uid", "admin-1") }))
            };

            _controller = new LoanManagementController(_mockLoanService.Object, _mockInstallmentService.Object)
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
            _mockLoanService.Setup(s => s.ClientHasActiveLoanAsync("client1")).ReturnsAsync(true);

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
            _mockLoanService.Setup(s => s.ClientHasActiveLoanAsync("client1")).ReturnsAsync(false);
            _mockLoanService.Setup(s => s.EvaluateRiskAsync("client1", 1000, 10, 12))
                .ReturnsAsync((true, 500m, 800m));

            // Act
            var result = await _controller.Assign(model);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var returnedModel = viewResult.Model.Should().BeOfType<AssignLoanViewModel>().Subject;
            returnedModel.IsHighRisk.Should().BeTrue();
            _mockLoanService.Verify(s => s.AssignAsync(It.IsAny<ABP.Core.Application.DTOs.Loan.AssignLoanDto>()), Times.Never);
        }

        [Fact]
        public async Task Details_ShouldReturnNotFound_WhenLoanDoesNotExist()
        {
            _mockLoanService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((ABP.Core.Application.DTOs.Loan.LoanDto?)null!);

            var result = await _controller.Details(99);

            result.Should().BeOfType<NotFoundResult>();
        }
    }
}
