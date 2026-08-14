using ABP.API.Controllers.v1.Admin;
using ABP.API.DTOs.Loan;
using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.Interfaces.IServices;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace ABP.Unit.Tests.Controllers
{
    public class LoanApiControllerTests
    {
        private readonly Mock<ILoanService> _mockLoanService;
        private readonly LoanApiController _controller;

        public LoanApiControllerTests()
        {
            _mockLoanService = new Mock<ILoanService>();

            _controller = new LoanApiController(_mockLoanService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("uid", "admin-1") }))
                    }
                }
            };
        }

        [Fact]
        public async Task Assign_ShouldReturnBadRequest_WhenTermIsNotAllowed()
        {
            // Arrange
            var request = new AssignLoanApiDto("client1", 1000, 10, 7);

            // Act
            var result = await _controller.Assign(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Assign_ShouldReturnBadRequest_WhenClientAlreadyHasActiveLoan()
        {
            // Arrange
            var request = new AssignLoanApiDto("client1", 1000, 10, 12);
            _mockLoanService.Setup(s => s.ClientHasActiveLoanAsync("client1")).ReturnsAsync(true);

            // Act
            var result = await _controller.Assign(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Assign_ShouldReturnConflict_WhenClientIsHighRiskAndNotConfirmed()
        {
            // Arrange
            var request = new AssignLoanApiDto("client1", 1000, 10, 12, ConfirmHighRisk: false);
            _mockLoanService.Setup(s => s.ClientHasActiveLoanAsync("client1")).ReturnsAsync(false);
            _mockLoanService.Setup(s => s.EvaluateRiskAsync("client1", 1000, 10, 12))
                .ReturnsAsync((true, 500m, 800m));

            // Act
            var result = await _controller.Assign(request);

            // Assert
            result.Should().BeOfType<ConflictObjectResult>();
            _mockLoanService.Verify(s => s.AssignAsync(It.IsAny<AssignLoanDto>()), Times.Never);
        }

        [Fact]
        public async Task Assign_ShouldCreateLoan_WhenHighRiskIsConfirmed()
        {
            // Arrange
            var request = new AssignLoanApiDto("client1", 1000, 10, 12, ConfirmHighRisk: true);
            _mockLoanService.Setup(s => s.ClientHasActiveLoanAsync("client1")).ReturnsAsync(false);
            _mockLoanService.Setup(s => s.EvaluateRiskAsync("client1", 1000, 10, 12))
                .ReturnsAsync((true, 500m, 800m));
            _mockLoanService.Setup(s => s.AssignAsync(It.IsAny<AssignLoanDto>()))
                .ReturnsAsync(new LoanDto { Id = 1, LoanNumber = "123456789" });

            // Act
            var result = await _controller.Assign(request);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(201);
        }

        [Fact]
        public async Task UpdateRate_ShouldReturnBadRequest_WhenRateIsNegative()
        {
            // Act
            var result = await _controller.UpdateRate(1, new UpdateRateRequest(-5));

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
