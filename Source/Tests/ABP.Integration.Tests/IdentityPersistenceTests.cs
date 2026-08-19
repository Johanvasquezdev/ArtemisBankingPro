using ABP.Core.Domain.Enums;
using ABP.Infraestructure.identity.Context;
using ABP.Infraestructure.identity.Entities;
using ABP.Infraestructure.identity.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
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

    [Fact]
    public async Task IdentityManagers_ShouldHashPasswordAssignRoleAndManageUserTokenLifecycle()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddDbContext<IdentityContext>(options => options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
            })
            .AddEntityFrameworkStores<IdentityContext>()
            .AddDefaultTokenProviders();

        await using var provider = services.BuildServiceProvider();
        var userManager = provider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = provider.GetRequiredService<RoleManager<IdentityRole>>();
        var role = new IdentityRole(UserRole.Client.ToString());
        (await roleManager.CreateAsync(role)).Succeeded.Should().BeTrue();

        var user = new ApplicationUser
        {
            UserName = "identity-lifecycle",
            Email = "identity-lifecycle@test.local",
            FirstName = "Identity",
            LastName = "Lifecycle",
            Cedula = "40200000099",
            Role = UserRole.Client,
            IsActive = true,
            EmailConfirmed = true
        };
        (await userManager.CreateAsync(user, "123Pa$$word!")).Succeeded.Should().BeTrue();
        (await userManager.AddToRoleAsync(user, UserRole.Client.ToString())).Succeeded.Should().BeTrue();

        user.PasswordHash.Should().NotBe("123Pa$$word!");
        (await userManager.CheckPasswordAsync(user, "123Pa$$word!")).Should().BeTrue();
        (await userManager.IsInRoleAsync(user, UserRole.Client.ToString())).Should().BeTrue();

        (await userManager.SetAuthenticationTokenAsync(user, "Artemis", "Refresh", "token-value"))
            .Succeeded.Should().BeTrue();
        (await userManager.GetAuthenticationTokenAsync(user, "Artemis", "Refresh"))
            .Should().Be("token-value");
        (await userManager.RemoveAuthenticationTokenAsync(user, "Artemis", "Refresh"))
            .Succeeded.Should().BeTrue();
        (await userManager.GetAuthenticationTokenAsync(user, "Artemis", "Refresh"))
            .Should().BeNull();
    }
}
