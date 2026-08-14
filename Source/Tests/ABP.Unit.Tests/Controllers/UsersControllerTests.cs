using ABP.API.Controllers.v1.Admin;
using ABP.API.DTOs.User;
using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Interfaces.IServices;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using Xunit;

namespace ABP.Unit.Tests.Controllers
{
    public class UsersControllerTests
    {
        private readonly Mock<IUserService> _mockUserService;
        private readonly Mock<IUserReadOnlyService> _mockUserReadOnlyService;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            _mockUserService = new Mock<IUserService>();
            _mockUserReadOnlyService = new Mock<IUserReadOnlyService>();

            _controller = new UsersController(_mockUserService.Object, _mockUserReadOnlyService.Object)
            {
                ControllerContext = new ControllerContext
                {
                    HttpContext = new DefaultHttpContext
                    {
                        User = new ClaimsPrincipal(new ClaimsIdentity([new Claim("uid", "admin-1")]))
                    }
                }
            };
        }

        [Fact]
        public async Task GetAll_ShouldReturnBadRequest_WhenPageSizeExceedsLimit()
        {
            // Act
            var result = await _controller.GetAll(1, 50);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task GetAll_ShouldReturnOk_WithPaginatedResult()
        {
            // Arrange
            var paged = new PaginatedResult<UserDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 20 };
            _mockUserReadOnlyService.Setup(s => s.GetAllAsync(1, 20, null)).ReturnsAsync(paged);

            // Act
            var result = await _controller.GetAll(1, 20, null);

            // Assert
            var okResult = result.Should().BeOfType<OkObjectResult>().Subject;
            okResult.Value.Should().Be(paged);
        }

        [Fact]
        public async Task Create_ShouldReturnBadRequest_WhenRoleIsCommerce()
        {
            // Arrange
            var request = new CreateUserRequest { Role = "Commerce", Cedula = "123", Email = "a@a.com", UserName = "u", Password = "p", ConfirmPassword = "p" };

            // Act
            var result = await _controller.Create(request);

            // Assert
            result.Should().BeOfType<BadRequestObjectResult>();
        }

        [Fact]
        public async Task Create_ShouldReturnConflict_WhenCedulaAlreadyExists()
        {
            // Arrange
            var request = new CreateUserRequest { Role = "Client", Cedula = "00187654321", Email = "a@a.com", UserName = "u", Password = "p", ConfirmPassword = "p" };
            _mockUserReadOnlyService.Setup(s => s.ExistsByCedulaAsync(request.Cedula, null)).ReturnsAsync(true);

            // Act
            var result = await _controller.Create(request);

            // Assert
            result.Should().BeOfType<ConflictObjectResult>();
        }

        [Fact]
        public async Task ChangeStatus_ShouldReturnForbid_WhenAdminTriesToChangeOwnStatus()
        {
            // Arrange
            var request = new ChangeUserStatusRequest { Status = false };

            // Act
            var result = await _controller.ChangeStatus("admin-1", request);

            // Assert
            result.Should().BeOfType<ForbidResult>();
        }
    }
}
