using ABP.API.Controllers.v1.Admin;
using ABP.API.DTOs.Loan;
using ABP.Core.Application.DTOs.Loan;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.Features.Admin.Commands;
using ABP.Core.Application.Features.Admin.Queries;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace ABP.Unit.Tests.Controllers
{
    public class LoanApiControllerTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly LoanController _controller;

        public LoanApiControllerTests()
        {
            _mockMediator = new Mock<IMediator>();

            _controller = new LoanController(_mockMediator.Object)
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
            result.Should().BeOfType<ObjectResult>();
        }

        [Fact]
        public async Task Assign_ShouldReturnBadRequest_WhenClientAlreadyHasActiveLoan()
        {
            // Arrange
            var request = new AssignLoanApiDto("client1", 1000, 10, 12);
            _mockMediator.Setup(m => m.Send(It.IsAny<AssignLoanCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LoanAssignmentResult(false, false, true, false, 0, 0, null, "El cliente ya tiene un préstamo activo."));

            // Act
            var result = await _controller.Assign(request);

            // Assert
            result.Should().BeOfType<ObjectResult>();
        }

        [Fact]
        public async Task Assign_ShouldReturnConflict_WhenClientIsHighRiskAndNotConfirmed()
        {
            // Arrange
            var request = new AssignLoanApiDto("client1", 1000, 10, 12, ConfirmHighRisk: false);
            _mockMediator.Setup(m => m.Send(It.IsAny<AssignLoanCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LoanAssignmentResult(false, true, false, true, 500m, 800m, null, "Se requiere confirmar el riesgo."));

            // Act
            var result = await _controller.Assign(request);

            // Assert
            var problem = result.Should().BeOfType<ObjectResult>().Subject;
            problem.StatusCode.Should().Be(409);
            _mockMediator.Verify(m => m.Send(It.IsAny<AssignLoanCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task Assign_ShouldCreateLoan_WhenHighRiskIsConfirmed()
        {
            // Arrange
            var request = new AssignLoanApiDto("client1", 1000, 10, 12, ConfirmHighRisk: true);
            _mockMediator.Setup(m => m.Send(It.IsAny<AssignLoanCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new LoanAssignmentResult(true, false, false, true, 500m, 800m,
                    new LoanDto { Id = 1, LoanNumber = "123456789" }, "Préstamo asignado correctamente."));

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
            result.Should().BeOfType<ObjectResult>();
        }
    }
}
