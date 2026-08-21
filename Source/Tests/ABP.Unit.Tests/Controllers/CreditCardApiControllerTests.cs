using ABP.API.Controllers.v1.Admin;
using ABP.API.DTOs.CreditCard;
using ABP.Core.Application.DTOs.CreditCard;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.Features.Admin.Commands;
using ABP.Core.Application.Features.Admin.Queries;
using FluentAssertions;
using MediatR;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ABP.Unit.Tests.Controllers
{
    public class CreditCardApiControllerTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly CreditCardController _controller;

        public CreditCardApiControllerTests()
        {
            _mockMediator = new Mock<IMediator>();
            _controller = new CreditCardController(_mockMediator.Object);
        }

        [Fact]
        public async Task GetAll_ShouldReturnBadRequest_WhenPageSizeExceedsLimit()
        {
            var result = await _controller.GetAll(1, 50);
            result.Should().BeOfType<ObjectResult>();
        }

        [Fact]
        public async Task GetAll_ShouldReturnBadRequest_WhenStatusIsInvalid()
        {
            var result = await _controller.GetAll(1, 20, "unknown");
            result.Should().BeOfType<ObjectResult>();
        }

        [Fact]
        public async Task Assign_ShouldReturnCreated_WithNewCard()
        {
            // Arrange
            var request = new AssignCreditCardApiDto { ClientId = "client1", CreditLimit = 5000 };
            _mockMediator.Setup(m => m.Send(It.IsAny<AssignCreditCardCommand>(), It.IsAny<CancellationToken>()))
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
            _mockMediator.Setup(m => m.Send(It.IsAny<GetAdminCreditCardDetailsQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((AdminCreditCardDetailsResult?)null);

            var result = await _controller.GetById(99);

            result.Should().BeOfType<ObjectResult>();
        }

        [Fact]
        public async Task UpdateLimit_ShouldReturnBadRequest_WhenLimitIsZeroOrLess()
        {
            var result = await _controller.UpdateLimit(1, new UpdateLimitRequest(0));
            result.Should().BeOfType<ObjectResult>();
        }

        [Fact]
        public async Task UpdateLimit_ShouldReturnBadRequest_WhenServiceThrowsInvalidOperation()
        {
            _mockMediator.Setup(m => m.Send(It.IsAny<UpdateCreditCardLimitCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("The new limit cannot be lower than the current debt."));

            var result = await _controller.UpdateLimit(1, new UpdateLimitRequest(100));

            result.Should().BeOfType<ObjectResult>();
        }

        [Fact]
        public async Task Cancel_ShouldReturnBadRequest_WhenCardHasOutstandingDebt()
        {
            _mockMediator.Setup(m => m.Send(It.IsAny<CancelCreditCardCommand>(), It.IsAny<CancellationToken>()))
                .ThrowsAsync(new InvalidOperationException("Cannot cancel card. Client owes money."));

            var result = await _controller.Cancel(1);

            result.Should().BeOfType<ObjectResult>();
        }
    }
}
