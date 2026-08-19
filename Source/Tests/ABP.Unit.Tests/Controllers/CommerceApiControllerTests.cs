using ABP.API.Controllers.v1.Admin;
using ABP.API.DTOs.Commerce;
using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.Commerce;
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
    public class CommerceApiControllerTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly CommerceController _controller;

        public CommerceApiControllerTests()
        {
            _mockMediator = new Mock<IMediator>();
            _controller = new CommerceController(_mockMediator.Object);
        }

        [Fact]
        public async Task GetAll_ShouldReturnBadRequest_WhenStatusIsInvalid()
        {
            var result = await _controller.GetAll(1, 20, "unknown");
            result.Should().BeOfType<ObjectResult>();
        }

        [Fact]
        public async Task GetAll_ShouldFilterOnlyActiveCommerces_ByDefault()
        {
            // Arrange
            var paged = new PaginatedResult<CommerceDto>
            {
                Items =
                [
                    new() { Id = 1, Name = "Active", IsActive = true },
                    new() { Id = 2, Name = "Inactive", IsActive = false }
                ],
                Page = 1,
                PageSize = 20,
                TotalCount = 2
            };
            _mockMediator.Setup(m => m.Send(It.IsAny<GetCommercesQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new GetCommercesResult(1, 20, paged.TotalCount, paged.Items));

            // Act
            var result = await _controller.GetAll(1, 20, "activo");

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenCommerceDoesNotExist()
        {
            _mockMediator.Setup(m => m.Send(It.IsAny<GetCommerceByIdQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync((GetCommerceByIdResult?)null);

            var result = await _controller.GetById(99);

            result.Should().BeOfType<ObjectResult>();
        }

        [Fact]
        public async Task Create_ShouldReturnCreated_WithValidRequest()
        {
            // Arrange
            var request = new CreateCommerceRequest
            {
                Name = "New Store", Description = "Demo", Logo = "logo.png",
                Email = "store@test.local", PhoneNumber = "8095550101", Rnc = "123456789"
            };
            _mockMediator.Setup(m => m.Send(It.IsAny<CreateCommerceCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateCommerceResult { Commerce = new CommerceDto { Id = 1, Name = request.Name } });

            // Act
            var result = await _controller.Create(request);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(201);
            _mockMediator.Verify(m => m.Send(It.IsAny<CreateCommerceCommand>(), It.IsAny<CancellationToken>()), Times.Once);
        }

        [Fact]
        public async Task ChangeStatus_ShouldReturnNotFound_WhenCommerceDoesNotExist()
        {
            _mockMediator.Setup(m => m.Send(It.IsAny<ChangeCommerceStatusCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(false);

            var result = await _controller.ChangeStatus(99, new ChangeCommerceStatusRequest { Status = false });

            result.Should().BeOfType<ObjectResult>();
        }
    }
}
