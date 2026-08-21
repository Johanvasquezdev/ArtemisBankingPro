using System.Security.Claims;
using ABP.Core.Application.DTOs;
using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.Features.Admin.Queries;
using ABP.Core.Application.Features.Admin.Commands;
using ABP.Core.Application.ViewModels.User;
using ABP.Core.Domain.Enums;
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
    public class UserManagementControllerTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly UserManagementController _controller;

        public UserManagementControllerTests()
        {
            _mockMediator = new Mock<IMediator>();

            var httpContext = new DefaultHttpContext
            {
                User = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim("uid", "admin-1") }))
            };

            _controller = new UserManagementController(_mockMediator.Object)
            {
                ControllerContext = new ControllerContext { HttpContext = httpContext },
                TempData = new TempDataDictionary(httpContext, Mock.Of<ITempDataProvider>())
            };
        }

        [Fact]
        public async Task Index_ShouldReturnView_WithPaginatedUsers()
        {
            // Arrange
            var paged = new PaginatedResult<UserDto> { Items = [], TotalCount = 0, Page = 1, PageSize = 20 };
            _mockMediator.Setup(m => m.Send(It.IsAny<GetAdminUsersQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(paged);

            // Act
            var result = await _controller.Index(1, null);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            viewResult.Model.Should().Be(paged);
        }

        [Fact]
        public async Task Create_ShouldReturnViewWithError_WhenCedulaAlreadyExists()
        {
            // Arrange
            var model = new SaveUserViewModel
            {
                FirstName = "Juan",
                LastName = "Perez",
                Cedula = "00187654321",
                Email = "j@a.com",
                Username = "juanp",
                Password = "P@ss1234",
                ConfirmPassword = "P@ss1234",
                Role = UserRole.Client
            };
            _mockMediator.Setup(m => m.Send(It.IsAny<CheckUserCedulaQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(true);

            // Act
            var result = await _controller.Create(model);

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var returnedModel = viewResult.Model.Should().BeOfType<SaveUserViewModel>().Subject;
            returnedModel.HasError.Should().BeTrue();
            _mockMediator.Verify(m => m.Send(It.IsAny<CreateUserCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task ChangeStatus_ShouldRedirectWithError_WhenAdminTriesToChangeOwnAccount()
        {
            // Act
            var result = await _controller.ChangeStatus("admin-1", false);

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["ErrorMessage"].Should().Be("No puede modificar el estado de su propia cuenta.");
            _mockMediator.Verify(m => m.Send(It.IsAny<ChangeUserStatusCommand>(), It.IsAny<CancellationToken>()), Times.Never);
        }

        [Fact]
        public async Task Edit_ShouldRedirectWithError_WhenAdminTriesToEditOwnAccount()
        {
            // Act
            var result = await _controller.Edit("admin-1");

            // Assert
            result.Should().BeOfType<RedirectToActionResult>();
            _controller.TempData["ErrorMessage"].Should().Be("No puede editar su propia cuenta desde este módulo.");
        }
    }
}
