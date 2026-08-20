using ABP.Core.Application.DTOs.Account;
using ABP.Core.Application.DTOs.Commerce;
using ABP.Core.Application.DTOs.SavingsAccount;
using ABP.Core.Application.DTOs.User;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Core.Domain.Enums;
using ABP.Infraestructure.Shared.EmailServices;
using ABP.Infraestructure.Shared.EmailServices.IEmailService;
using ABP.Infraestructure.identity.Context;
using ABP.Infraestructure.identity.Entities;
using ABP.Infraestructure.identity.Seeds;
using ABP.Infraestructure.identity.Services;
using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Moq;
using Xunit;

namespace ABP.Integration.Tests.Identity;

public sealed class IdentityServiceIntegrationTests : IDisposable
{
    private readonly ServiceProvider _provider;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly RoleManager<IdentityRole> _roleManager;
    private readonly Mock<ICorreoServices> _email = new();
    private readonly Mock<ISavingsAccountService> _savings = new();
    private readonly Mock<ICommerceService> _commerce = new();

    public IdentityServiceIntegrationTests()
    {
        var configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["ApplicationUrl"] = "https://test.artemis.local",
                ["JWT:Key"] = "this-is-a-test-key-with-at-least-32-characters"
            })
            .Build();

        var services = new ServiceCollection();
        services.AddLogging();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddHttpContextAccessor();
        services.AddDbContext<IdentityContext>(options =>
            options.UseInMemoryDatabase(Guid.NewGuid().ToString()));
        services.AddIdentity<ApplicationUser, IdentityRole>(options =>
            {
                options.Password.RequireDigit = true;
                options.Password.RequireLowercase = true;
                options.Password.RequireUppercase = true;
                options.Password.RequireNonAlphanumeric = true;
                options.Password.RequiredLength = 8;
                options.User.RequireUniqueEmail = true;
                options.SignIn.RequireConfirmedEmail = true;
            })
            .AddEntityFrameworkStores<IdentityContext>()
            .AddDefaultTokenProviders();

        _provider = services.BuildServiceProvider();
        _userManager = _provider.GetRequiredService<UserManager<ApplicationUser>>();
        _roleManager = _provider.GetRequiredService<RoleManager<IdentityRole>>();
        _email.Setup(service => service.SendEmailAsync(It.IsAny<EmailRequest>()))
            .Returns(Task.CompletedTask);
        _savings.Setup(service => service.CreateAccountAsync(
                It.IsAny<string>(), It.IsAny<string>(), It.IsAny<decimal>(), It.IsAny<AccountType>()))
            .ReturnsAsync(new SavingsAccountDto());
    }

    [Fact]
    public async Task DefaultRoles_ShouldCreateEveryDomainRole()
    {
        await DefaultRoles.SeedAsync(_roleManager);

        foreach (var role in Enum.GetNames<UserRole>())
        {
            (await _roleManager.RoleExistsAsync(role)).Should().BeTrue();
        }
    }

    [Fact]
    public async Task DefaultUsers_ShouldCreateActiveConfirmedUsersForAdminCashierAndClient()
    {
        await DefaultRoles.SeedAsync(_roleManager);
        await DefaultUsers.SeedAsync(_userManager);

        var users = await _userManager.Users.ToListAsync();
        users.Should().HaveCount(4);
        
        var activeUsers = users.Where(u => u.Role != UserRole.Commerce).ToList();
        activeUsers.Should().OnlyContain(user => user.IsActive && user.EmailConfirmed);
        
        var commerceUser = users.Single(u => u.Role == UserRole.Commerce);
        commerceUser.IsActive.Should().BeTrue();
        commerceUser.EmailConfirmed.Should().BeTrue();
        
        users.Select(user => user.Role).Should().BeEquivalentTo(
            [UserRole.Admin, UserRole.Cashier, UserRole.Client, UserRole.Commerce]);
    }

    [Fact]
    public async Task RegisterClient_ShouldCreateInactiveUserAndSendWebActivationEmail()
    {
        await SeedRoleAsync(UserRole.Client);
        var service = CreateUserService();

        var result = await service.RegisterAsync(
            "Ada", "Artemis", "40200000001", "ada-client", "ada@test.local",
            "123Pa$$word!", UserRole.Client.ToString(), "admin-1", 250,
            AccountEmailChannel.Web);

        result.Should().BeTrue();
        var user = await _userManager.FindByNameAsync("ada-client");
        user.Should().NotBeNull();
        user!.IsActive.Should().BeFalse();
        user.EmailConfirmed.Should().BeFalse();
        user.ActivationToken.Should().NotBeNullOrWhiteSpace();
        (await _userManager.IsInRoleAsync(user, UserRole.Client.ToString())).Should().BeTrue();
        _savings.Verify(service => service.CreateAccountAsync(
            user.Id, "admin-1", 250, AccountType.Primary), Times.Once);
        _email.Verify(service => service.SendEmailAsync(It.Is<EmailRequest>(email =>
            email.IsHtml && email.Body.Contains("Activar mi cuenta") &&
            email.Body.Contains("ada-client"))), Times.Once);
    }

    [Fact]
    public async Task UpdateClient_WithAdditionalAmount_ShouldDepositIntoPrimaryAccount()
    {
        await SeedRoleAsync(UserRole.Client);
        var user = await CreateUserAsync("update-client", UserRole.Client, true, true);
        var primary = new SavingsAccountDto
        {
            AccountNumber = "572787583",
            Status = AccountStatus.Active,
            Balance = 1000,
            Type = AccountType.Primary,
            UserId = user.Id
        };
        _savings.Setup(service => service.GetPrimaryAccountByClientIdAsync(user.Id))
            .ReturnsAsync(primary);
        _savings.Setup(service => service.DepositAsync(primary.AccountNumber, 200))
            .ReturnsAsync(true);

        var updated = await CreateUserService().UpdateAsync(new UpdateUserDto
        {
            Id = user.Id,
            FirstName = "Updated",
            LastName = "Client",
            Cedula = user.Cedula,
            Email = user.Email!,
            Username = user.UserName!,
            AdditionalAmount = 200
        });

        updated.Should().BeTrue();
        _savings.Verify(service => service.DepositAsync(primary.AccountNumber, 200), Times.Once);
    }

    [Fact]
    public async Task UpdateClient_WithAdditionalAmountAndNoPrimaryAccount_ShouldExplainBusinessRule()
    {
        await SeedRoleAsync(UserRole.Client);
        var user = await CreateUserAsync("missing-primary", UserRole.Client, true, true);
        _savings.Setup(service => service.GetPrimaryAccountByClientIdAsync(user.Id))
            .ReturnsAsync((SavingsAccountDto?)null);

        var act = () => CreateUserService().UpdateAsync(new UpdateUserDto
        {
            Id = user.Id,
            FirstName = user.FirstName,
            LastName = user.LastName,
            Cedula = user.Cedula,
            Email = user.Email!,
            Username = user.UserName!,
            AdditionalAmount = 200
        });

        var exception = await act.Should().ThrowAsync<InvalidOperationException>();
        exception.Which.Message.Should().Contain("cuenta principal de ahorro activa");
        _savings.Verify(service => service.DepositAsync(It.IsAny<string>(), It.IsAny<decimal>()), Times.Never);
    }

    [Fact]
    public async Task RegisterCommerceUser_ShouldRequireActiveCommerceAndPreventDuplicateAssociation()
    {
        await SeedRoleAsync(UserRole.Commerce);
        _commerce.Setup(service => service.GetByIdAsync(7))
            .ReturnsAsync(new CommerceDto { Id = 7, IsActive = true });
        var service = CreateUserService();

        var created = await service.RegisterCommerceUserAsync(
            "Commerce", "User", "40200000002", "commerce-user", "commerce@test.local",
            "123Pa$$word!", 7, AccountEmailChannel.Api);
        var duplicate = await service.RegisterCommerceUserAsync(
            "Other", "User", "40200000003", "commerce-user-2", "commerce2@test.local",
            "123Pa$$word!", 7, AccountEmailChannel.Api);

        created.Should().BeTrue();
        duplicate.Should().BeFalse();
        var user = await _userManager.FindByNameAsync("commerce-user");
        user.Should().NotBeNull();
        user!.Role.Should().Be(UserRole.Commerce);
        user.CommerceId.Should().Be(7);
        user.IsActive.Should().BeFalse();
        _savings.Verify(service => service.CreateAccountAsync(
            user.Id, "SYSTEM", 0, AccountType.Primary), Times.Once);
        _email.Verify(service => service.SendEmailAsync(It.IsAny<EmailRequest>()), Times.Once);

        _commerce.Setup(service => service.GetByIdAsync(8))
            .ReturnsAsync(new CommerceDto { Id = 8, IsActive = false });
        (await service.RegisterCommerceUserAsync(
            "Inactive", "Commerce", "40200000004", "inactive-commerce", "inactive@test.local",
            "123Pa$$word!", 8)).Should().BeFalse();
    }

    [Fact]
    public async Task Authenticate_ShouldRejectInactiveAndUnconfirmedUsers()
    {
        await SeedRoleAsync(UserRole.Client);
        var unconfirmed = await CreateUserAsync("unconfirmed", UserRole.Client, isActive: false, emailConfirmed: false);
        var service = CreateUserService();

        var unconfirmedResult = await service.AuthenticateAsync(unconfirmed.UserName!, "123Pa$$word!");
        unconfirmedResult.Success.Should().BeFalse();
        unconfirmedResult.Error.Should().Contain("confirmada");

        var inactive = await CreateUserAsync("inactive", UserRole.Client, isActive: false, emailConfirmed: true);
        var inactiveResult = await service.AuthenticateAsync(inactive.UserName!, "123Pa$$word!");
        inactiveResult.Success.Should().BeFalse();
        inactiveResult.Error.Should().Contain("inactiv");
    }

    [Fact]
    public async Task ActivateAccount_ShouldConfirmAndActivateOnlyWithStoredToken()
    {
        await SeedRoleAsync(UserRole.Client);
        var user = await CreateUserAsync("activation", UserRole.Client, isActive: false, emailConfirmed: false);
        user.ActivationToken = await _userManager.GenerateEmailConfirmationTokenAsync(user);
        await _userManager.UpdateAsync(user);
        var service = CreateUserService();

        (await service.ActivateAccountAsync("invalid-token")).Should().BeFalse();
        (await service.ActivateAccountAsync(user.ActivationToken!)).Should().BeTrue();

        var activated = await _userManager.FindByIdAsync(user.Id);
        activated!.IsActive.Should().BeTrue();
        activated.EmailConfirmed.Should().BeTrue();
        activated.ActivationToken.Should().BeNull();
    }

    [Fact]
    public async Task ChangeStatus_ShouldRequireAdminAndProtectCommerceAssociation()
    {
        await SeedRoleAsync(UserRole.Admin);
        await SeedRoleAsync(UserRole.Commerce);
        var admin = await CreateUserAsync("status-admin", UserRole.Admin, isActive: true, emailConfirmed: true);
        var activeCommerce = await CreateUserAsync("active-commerce", UserRole.Commerce, true, true, 9);
        var inactiveCommerce = await CreateUserAsync("inactive-commerce-2", UserRole.Commerce, false, false, 9);
        _commerce.Setup(service => service.GetByIdAsync(9))
            .ReturnsAsync(new CommerceDto { Id = 9, IsActive = true });
        var service = CreateUserService();

        (await service.ChangeStatusAsync("not-an-admin", inactiveCommerce.Id, true)).Should().BeFalse();
        (await service.ChangeStatusAsync(admin.Id, inactiveCommerce.Id, true)).Should().BeFalse();
        (await service.ChangeStatusAsync(admin.Id, activeCommerce.Id, false)).Should().BeTrue();
        (await service.ChangeStatusAsync(admin.Id, inactiveCommerce.Id, true)).Should().BeTrue();

        var updated = await _userManager.FindByIdAsync(inactiveCommerce.Id);
        updated!.IsActive.Should().BeTrue();
        updated.EmailConfirmed.Should().BeTrue();
    }

    [Fact]
    public async Task PasswordReset_ShouldUseApiTokenAndReactivateUserAfterValidReset()
    {
        await SeedRoleAsync(UserRole.Client);
        var user = await CreateUserAsync("reset-user", UserRole.Client, true, true);
        var service = CreateUserService();

        (await service.GeneratePasswordResetTokenAsync(user.UserName!, AccountEmailChannel.Api)).Should().BeTrue();
        var pending = await _userManager.FindByIdAsync(user.Id);
        pending!.IsActive.Should().BeFalse();
        pending.ActivationToken.Should().NotBeNullOrWhiteSpace();
        _email.Verify(email => email.SendEmailAsync(It.Is<EmailRequest>(request =>
            request.Body.Contains("UserId:") && request.Body.Contains("Token:"))), Times.Once);

        (await service.ResetPasswordAsync(user.UserName!, pending.ActivationToken!, "New123Pa$$word!"))
            .Should().BeTrue();
        var reset = await _userManager.FindByIdAsync(user.Id);
        reset!.IsActive.Should().BeTrue();
        reset.ActivationToken.Should().BeNull();
        (await _userManager.CheckPasswordAsync(reset, "New123Pa$$word!")).Should().BeTrue();
    }

    [Fact]
    public async Task CommerceUserDirectory_ShouldReturnOnlyActiveConfirmedCommerceUsers()
    {
        var options = new DbContextOptionsBuilder<IdentityContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        await using var context = new IdentityContext(options);
        context.Users.AddRange(
            new ApplicationUser
            {
                Id = "commerce-inactive",
                UserName = "commerce-inactive",
                Email = "commerce-inactive@test.local",
                Role = UserRole.Commerce,
                CommerceId = 21,
                IsActive = false,
                EmailConfirmed = true
            },
            new ApplicationUser
            {
                Id = "commerce-unconfirmed",
                UserName = "commerce-unconfirmed",
                Email = "commerce-unconfirmed@test.local",
                Role = UserRole.Commerce,
                CommerceId = 21,
                IsActive = true,
                EmailConfirmed = false
            },
            new ApplicationUser
            {
                Id = "commerce-active",
                UserName = "commerce-active",
                Email = "commerce-active@test.local",
                Role = UserRole.Commerce,
                CommerceId = 21,
                IsActive = true,
                EmailConfirmed = true
            });
        await context.SaveChangesAsync();

        var directory = new CommerceUserDirectory(context);

        (await directory.HasActiveUserAsync(21)).Should().BeTrue();
        (await directory.GetActiveUserIdAsync(21)).Should().Be("commerce-active");
        (await directory.GetActiveUserIdAsync(999)).Should().BeNull();

        var associated = await directory.GetAssociatedUserAsync(21);
        associated.Should().NotBeNull();
        associated!.Id.Should().BeOneOf("commerce-inactive", "commerce-unconfirmed", "commerce-active");
        associated.Email.Should().EndWith("@test.local");
    }

    private UserService CreateUserService() => new(
        _userManager,
        _provider.GetRequiredService<SignInManager<ApplicationUser>>(),
        _email.Object,
        _savings.Object,
        _commerce.Object,
        _provider.GetRequiredService<IHttpContextAccessor>(),
        _provider.GetRequiredService<IConfiguration>());

    private async Task SeedRoleAsync(UserRole role)
    {
        if (!await _roleManager.RoleExistsAsync(role.ToString()))
        {
            (await _roleManager.CreateAsync(new IdentityRole(role.ToString()))).Succeeded.Should().BeTrue();
        }
    }

    private async Task<ApplicationUser> CreateUserAsync(
        string username,
        UserRole role,
        bool isActive,
        bool emailConfirmed,
        int? commerceId = null)
    {
        var user = new ApplicationUser
        {
            UserName = username,
            Email = $"{username}@test.local",
            FirstName = "Test",
            LastName = "User",
            Cedula = $"{Math.Abs(username.GetHashCode()):00000000000}"[..11],
            Role = role,
            IsActive = isActive,
            EmailConfirmed = emailConfirmed,
            CommerceId = commerceId
        };
        (await _userManager.CreateAsync(user, "123Pa$$word!")).Succeeded.Should().BeTrue();
        (await _userManager.AddToRoleAsync(user, role.ToString())).Succeeded.Should().BeTrue();
        return user;
    }

    public void Dispose() => _provider.Dispose();
}
