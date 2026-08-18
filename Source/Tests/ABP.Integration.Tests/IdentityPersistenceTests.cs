using ABP.Core.Domain.Enums;
using ABP.Infraestructure.identity.Context;
using ABP.Infraestructure.identity.Entities;
using ABP.Infraestructure.identity.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using System.IdentityModel.Tokens.Jwt;
using Xunit;

namespace ABP.Integration.Tests;

public sealed class IdentityPersistenceTests
{
    [Fact]
    public async Task IdentityContext_ShouldPersistUserRoleAndTokenRelations()
    {
        var options = new DbContextOptionsBuilder<IdentityContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new IdentityContext(options);
        var user = new ApplicationUser
        {
            Id = "user-1", UserName = "client", NormalizedUserName = "CLIENT",
            Email = "client@test.local", NormalizedEmail = "CLIENT@TEST.LOCAL",
            FirstName = "Client", LastName = "Test", Cedula = "40200000000",
            Role = UserRole.Client, IsActive = true, EmailConfirmed = true
        };
        var role = new IdentityRole(UserRole.Client.ToString()) { Id = "role-client", NormalizedName = "CLIENT" };
        context.Users.Add(user);
        context.Roles.Add(role);
        context.UserRoles.Add(new IdentityUserRole<string> { UserId = user.Id, RoleId = role.Id });
        context.UserTokens.Add(new IdentityUserToken<string>
        {
            UserId = user.Id, LoginProvider = "Artemis", Name = "Activation", Value = "token-1"
        });
        await context.SaveChangesAsync();

        (await context.Users.SingleAsync(x => x.Id == "user-1")).Role.Should().Be(UserRole.Client);
        (await context.UserRoles.SingleAsync(x => x.UserId == "user-1")).RoleId.Should().Be("role-client");
        (await context.UserTokens.SingleAsync(x => x.UserId == "user-1")).Value.Should().Be("token-1");
    }

    [Fact]
    public async Task JwtService_ShouldIncludeIdentityAndCommerceClaims()
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(new Dictionary<string, string?>
        {
            ["JWT:Key"] = "this-is-a-test-key-with-at-least-32-characters",
            ["JWT:Issuer"] = "ArtemisBankingPro",
            ["JWT:Audience"] = "ArtemisBankingPro.Client",
            ["JWT:DurationInMinutes"] = "30"
        }).Build();

        var token = await new JwtService(configuration).GenerateTokenAsync(
            "user-1", "client", "client@test.local", [UserRole.Client.ToString()], 42);
        var jwt = new JwtSecurityTokenHandler().ReadJwtToken(token);

        jwt.Claims.Should().Contain(x => x.Type == JwtRegisteredClaimNames.Sub && x.Value == "user-1");
        jwt.Claims.Should().Contain(x => x.Type == System.Security.Claims.ClaimTypes.Role && x.Value == "Client");
        jwt.Claims.Should().Contain(x => x.Type == "commerceId" && x.Value == "42");
        jwt.ValidTo.Should().BeAfter(DateTime.UtcNow);
    }
}
