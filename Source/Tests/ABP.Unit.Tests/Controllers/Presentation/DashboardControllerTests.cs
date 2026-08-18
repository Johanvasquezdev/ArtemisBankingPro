using ABP.Core.Application.DTOs.Dashboard;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Application.Features.Admin.Queries;
using ABP.Core.Application.ViewModels.Dashboard;
using ArtemisBankingPro.Areas.Admin.Controllers;
using FluentAssertions;
using Microsoft.AspNetCore.Mvc;
using Moq;
using MediatR;
using Xunit;

namespace ABP.Unit.Tests.Controllers.Presentation
{
    public class DashboardControllerTests
    {
        private readonly Mock<IMediator> _mockMediator;
        private readonly DashboardController _controller;

        public DashboardControllerTests()
        {
            _mockMediator = new Mock<IMediator>();
            _controller = new DashboardController(_mockMediator.Object);
        }

        [Fact]
        public async Task Index_ShouldReturnView_WithMappedAdminDashboardViewModel()
        {
            // Arrange
            var dto = new DashboardAdminDto
            {
                TotalTransactions = 100,
                TodayTransactions = 5,
                TotalProducts = 20,
                ActiveLoans = 4,
                ActiveCreditCards = 6,
                TotalSavingsAccounts = 10,
                TodayPayments = 2,
                ActiveClients = 15,
                InactiveClients = 3,
                AverageDebt = 2500.50m
            };
            _mockMediator.Setup(m => m.Send(It.IsAny<GetAdminDashboardQuery>(), It.IsAny<CancellationToken>()))
                .ReturnsAsync(dto);

            // Act
            var result = await _controller.Index();

            // Assert
            var viewResult = result.Should().BeOfType<ViewResult>().Subject;
            var model = viewResult.Model.Should().BeOfType<AdminDashboardViewModel>().Subject;

            model.TotalTransactions.Should().Be(100);
            model.TodayTransactions.Should().Be(5);
            model.ActiveLoans.Should().Be(4);
            model.ActiveCreditCards.Should().Be(6);
            model.TotalSavingsAccounts.Should().Be(10);
            model.TotalActiveClients.Should().Be(15);
            model.TotalInactiveClients.Should().Be(3);
            model.AverageDebt.Should().Be(2500.50m);
        }
    }
}
