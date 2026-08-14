using ABP.API.Controllers.v1.Admin;
using ABP.API.DTOs.CreditCard;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.Interfaces.IServices;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ABP.Unit.Tests.Controllers
{
    public class CreditCardApiControllerTests
    {
        private readonly Mock<ICreditCardService> _mockCardService;
        private readonly Mock<ICreditCardConsumptionService> _mockConsumptionService;
        private readonly CreditCardApiController _controller;

        public CreditCardApiControllerTests()
        {
            _mockCardService = new Mock<ICreditCardService>();
            _mockConsumptionService = new Mock<ICreditCardConsumptionService>();
            _controller = new CreditCardApiController(_mockCardService.Object, _mockConsumptionService.Object);
        }

        [Fact]
        public async Task GetAll_ShouldReturnBadRequest_WhenPageSizeExceedsLimit()
        {
            var result = await _controller.GetAll(1, 50);
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetAll_ShouldReturnBadRequest_WhenStatusIsInvalid()
        {
            var result = await _controller.GetAll(1, 20, "unknown");
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Assign_ShouldReturnCreated_WithNewCard()
        {
            // Arrange
            var request = new AssignCreditCardApiDto { ClientId = "client1", CreditLimit = 5000 };
            _mockCardService.Setup(s => s.AssignAsync(It.IsAny<AssignCreditCardDto>()))
                .ReturnsAsync(new CreditCardDto { Id = 1, CreditLimit = 5000 });

            // Act
            var result = await _controller.Assign(request);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(201);
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenCardDoesNotExist()
        {
            _mockCardService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((CreditCardDto?)null!);

            var result = await _controller.GetById(99);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task UpdateLimit_ShouldReturnBadRequest_WhenLimitIsZeroOrLess()
        {
            var result = await _controller.UpdateLimit(1, new UpdateLimitRequest(0));
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task UpdateLimit_ShouldReturnBadRequest_WhenServiceThrowsInvalidOperation()
        {
            _mockCardService.Setup(s => s.UpdateLimitAsync(1, 100))
                .ThrowsAsync(new InvalidOperationException("The new limit cannot be lower than the current debt."));

            var result = await _controller.UpdateLimit(1, new UpdateLimitRequest(100));

            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Cancel_ShouldReturnBadRequest_WhenCardHasOutstandingDebt()
        {
            _mockCardService.Setup(s => s.CancelAsync(1))
                .ThrowsAsync(new InvalidOperationException("Cannot cancel card. Client owes money."));

            var result = await _controller.Cancel(1);

            result.Should().BeOfType<BadRequestObjectResult>();
        }
    }
}
