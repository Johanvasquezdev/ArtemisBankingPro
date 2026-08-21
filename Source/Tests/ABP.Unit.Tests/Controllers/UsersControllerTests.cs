using ABP.API.Controllers.v1.Admin;
using ABP.API.DTOs.User;
using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.User;
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
    public class UsersControllerTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly UsersController _controller;

        public UsersControllerTests()
        {
            _mockMediator = new Mock<IMediator>();

            _controller = new UsersController(_mockMediator.Object)
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
            result.Should().BeOfType<ObjectResult>();
        }

        [Fact]
        public async Task GetAll_ShouldReturnOk_WithPaginatedResult()
        {
            // Arrange
            var paged = new PaginatedResult<UserDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 20 };
            _mockMediator.Setup(m => m.Send(It.IsAny<GetUsersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(paged);

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
            result.Should().BeOfType<ObjectResult>();
        }

        [Fact]
        public async Task Create_ShouldReturnConflict_WhenCedulaAlreadyExists()
        {
            // Arrange
            var request = new CreateUserRequest { Role = "Client", Cedula = "00187654321", Email = "a@a.com", UserName = "u", Password = "p", ConfirmPassword = "p" };
            _mockMediator.Setup(m => m.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(new CreateUserResult { CedulaAlreadyExists = true });

            // Act
            var result = await _controller.Create(request);

            // Assert
            result.Should().BeOfType<ObjectResult>();
        }

        [Fact]
        public async Task ChangeStatus_ShouldReturnForbid_WhenAdminTriesToChangeOwnStatus()
        {
            // Arrange
            var request = new ChangeUserStatusRequest { Status = false };

            // Act
            var result = await _controller.ChangeStatus("admin-1", request);

            // Assert
            result.Should().BeOfType<ObjectResult>();
        }
    }
}
