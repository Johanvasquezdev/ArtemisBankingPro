using ABP.API.Controllers.v1;
using ABP.Core.Application.DTOs.Common;
using ABP.Core.Application.Features.Commerce.Queries;
using ABP.Core.Domain.Interfaces;
using MediatR;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Moq;
using System.Security.Claims;
using System.Threading;
using System.Threading.Tasks;
using Xunit;
using FluentAssertions;

using FluentValidation.TestHelper;

namespace ABP.Unit.Tests.Api;

public class HermesPayControllerTests
{
    [Fact]
    public async Task GetTransactions_MissingCommerceIdClaim_ShouldReturnProblemDetailsForbidden()
    {
        var mediator = new Mock<IMediator>();
        var commerceRepo = new Mock<ICommerceRepository>();
        var controller = new HermesPayController(mediator.Object, commerceRepo.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { new Claim(ClaimTypes.Role, "Commerce") }));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };

        var result = await controller.GetTransactions(1, 1, 20);

        var problem = result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(StatusCodes.Status403Forbidden);
        problem.Value.Should().BeOfType<ProblemDetails>()
            .Which.Title.Should().Be("Acceso denegado");
    }

    [Fact]
    public void GetTransactionsQueryValidator_InvalidPageOrSize_ShouldHaveValidationErrors()
    {
        var validator = new GetCommerceTransactionsQueryValidator();
        
        var queryPage = new GetCommerceTransactionsQuery(10, 0, 20);
        var resultPage = validator.TestValidate(queryPage);
        resultPage.ShouldHaveValidationErrorFor(x => x.Page);

        var querySize = new GetCommerceTransactionsQuery(10, 1, 21);
        var resultSize = validator.TestValidate(querySize);
        resultSize.ShouldHaveValidationErrorFor(x => x.PageSize);
    }

    [Fact]
    public async Task GetTransactions_InactiveCommerce_ShouldReturnProblemDetailsNotFound()
    {
        var mediator = new Mock<IMediator>();
        var commerceRepo = new Mock<ICommerceRepository>();
        var controller = new HermesPayController(mediator.Object, commerceRepo.Object);
        var user = new ClaimsPrincipal(new ClaimsIdentity(new[] { 
            new Claim(ClaimTypes.Role, "Commerce"),
            new Claim("commerceId", "10") 
        }));
        controller.ControllerContext = new ControllerContext { HttpContext = new DefaultHttpContext { User = user } };
        
        commerceRepo.Setup(r => r.GetByIdAsync(10)).ReturnsAsync(new ABP.Core.Domain.Entities.Commerce { Id = 10, IsActive = false });

        var result = await controller.GetTransactions(10, 1, 20);
        
        var problem = result.Should().BeOfType<ObjectResult>().Subject;
        problem.StatusCode.Should().Be(StatusCodes.Status404NotFound);
        problem.Value.Should().BeOfType<ProblemDetails>()
            .Which.Title.Should().Be("Comercio no encontrado");
    }
}
