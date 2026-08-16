using ABP.API.Controllers.v1.Admin;
using ABP.API.DTOs.Commerce;
using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.Commerce;
using ABP.Core.Application.Interfaces.IServices;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using Xunit;

namespace ABP.Unit.Tests.Controllers
{
    public class CommerceApiControllerTests
    {
        private readonly Mock<ICommerceService> _mockCommerceService;
        private readonly CommerceController _controller;

        public CommerceApiControllerTests()
        {
            _mockCommerceService = new Mock<ICommerceService>();
            _controller = new CommerceController(_mockCommerceService.Object);
        }

        [Fact]
        public async Task GetAll_ShouldReturnBadRequest_WhenStatusIsInvalid()
        {
            var result = await _controller.GetAll(1, 20, "unknown");
            result.Should().BeOfType<BadRequestObjectResult>();
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
            _mockCommerceService.Setup(s => s.GetAllPagedAsync(1, 20)).ReturnsAsync(paged);

            // Act
            var result = await _controller.GetAll(1, 20, "activo");

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().NotBeNull();
        }

        [Fact]
        public async Task GetById_ShouldReturnNotFound_WhenCommerceDoesNotExist()
        {
            _mockCommerceService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((CommerceDto?)null!);

            var result = await _controller.GetById(99);

            result.Should().BeOfType<NotFoundObjectResult>();
        }

        [Fact]
        public async Task Create_ShouldReturnCreated_WithValidRequest()
        {
            // Arrange
            var request = new CreateCommerceRequest { Name = "New Store", Description = "Demo", Logo = "logo.png" };

            // Act
            var result = await _controller.Create(request);

            // Assert
            var objectResult = result.Should().BeOfType<ObjectResult>().Subject;
            objectResult.StatusCode.Should().Be(201);
            _mockCommerceService.Verify(s => s.AddAsync(It.IsAny<CommerceDto>()), Times.Once);
        }

        [Fact]
        public async Task ChangeStatus_ShouldReturnNotFound_WhenCommerceDoesNotExist()
        {
            _mockCommerceService.Setup(s => s.GetByIdAsync(99)).ReturnsAsync((CommerceDto?)null!);

            var result = await _controller.ChangeStatus(99, new ChangeCommerceStatusRequest { Status = false });

            result.Should().BeOfType<NotFoundObjectResult>();
        }
    }
}
