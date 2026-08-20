using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Security.Cryptography;
using System.Text;
using ABP.Core.Domain.Entities;
using ABP.Core.Domain.Enums;
using ABP.Core.Application.Interfaces.IServices;
using ABP.Infraestructure.Shared.EmailServices;
using ABP.Infraestructure.Shared.EmailServices.IEmailService;
using ABP.Infraestructure.Persistence.Context;
using ABP.Infraestructure.identity.Context;
using ABP.Infraestructure.identity.Entities;
using FluentAssertions;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Diagnostics;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.DependencyInjection.Extensions;
using Xunit;

namespace ABP.Integration.Tests.Api;

public sealed class ApiIntegrationTests : IClassFixture<ApiApplicationFactory>
{
    private readonly ApiApplicationFactory _factory;

    public ApiIntegrationTests(ApiApplicationFactory factory) => _factory = factory;

    [Fact]
    public async Task AdminEndpoint_ShouldReturnProblemDetailsWhenUnauthenticated()
    {
        using var client = CreateClient();

        using var response = await client.GetAsync("/api/v1/commerce");
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        body.GetProperty("status").GetInt32().Should().Be(401);
        body.GetProperty("title").GetString().Should().Be("Autenticación requerida");
        body.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    [Fact]
    public async Task AccountConfirmation_ShouldReturnValidationProblemDetailsForMissingToken()
    {
        using var client = CreateClient();

        using var response = await client.PostAsJsonAsync("/api/v1/Account/confirm", new { token = "" });
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();

        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        body.GetProperty("title").GetString().Should().Be("Token requerido");
        body.GetProperty("status").GetInt32().Should().Be(400);
    }

    [Fact]
    public async Task AccountEndpoints_ShouldReturnProblemDetailsForInvalidCredentialsAndResetRequests()
    {
        using var client = CreateClient();

        using var invalidLogin = await client.PostAsJsonAsync("/api/v1/Account/login", new
        {
            userName = "ClientUser",
            password = "wrong-password"
        });
        await AssertProblemDetailsAsync(invalidLogin, HttpStatusCode.Unauthorized, "Autenticación fallida");

        using var mismatch = await client.PostAsJsonAsync("/api/v1/Account/reset-password", new
        {
            userId = "user-1",
            token = "token",
            password = "New123Pa$$word!",
            confirmPassword = "Different123Pa$$word!"
        });
        await AssertProblemDetailsAsync(mismatch, HttpStatusCode.BadRequest, "Contraseñas no coinciden");

        using var unknown = await client.PostAsJsonAsync("/api/v1/Account/get-reset-token", new
        {
            userName = "does-not-exist"
        });
        await AssertProblemDetailsAsync(unknown, HttpStatusCode.NotFound, "Usuario no encontrado");

        using var denied = await client.GetAsync("/api/v1/Account/access-denied");
        await AssertProblemDetailsAsync(denied, HttpStatusCode.Forbidden, "Acceso denegado");
    }

    [Fact]
    public async Task AdminLoginAndCommerceCreation_ShouldCompleteThroughHttpPipeline()
    {
        using var client = CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/Account/login", new
        {
            userName = "AdminUser",
            password = "123Pa$$word!"
        });

        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var loginBody = await login.Content.ReadFromJsonAsync<JsonElement>();
        var token = loginBody.GetProperty("jwt").GetString();
        token.Should().NotBeNullOrWhiteSpace();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var rnc = $"{Random.Shared.Next(100000000, 999999999)}";
        using var create = await client.PostAsJsonAsync("/api/v1/commerce", new
        {
            name = "Integration Commerce",
            description = "Created through the API integration test",
            logo = "logo.svg",
            rnc,
            email = $"commerce-{Guid.NewGuid():N}@test.local",
            phoneNumber = "8095550101"
        });

        create.StatusCode.Should().Be(HttpStatusCode.Created);
        var commerce = await create.Content.ReadFromJsonAsync<JsonElement>();
        commerce.GetProperty("rnc").GetString().Should().Be(rnc);
        commerce.GetProperty("isActive").GetBoolean().Should().BeTrue();
    }

    [Fact]
    public async Task ClientToken_ShouldBeRejectedFromAdminEndpointWithProblemDetails()
    {
        var client = CreateClient();
        var login = await client.PostAsJsonAsync("/api/v1/Account/login", new
        {
            userName = "ClientUser",
            password = "123Pa$$word!"
        });
        login.StatusCode.Should().Be(HttpStatusCode.Forbidden);
    }

    [Fact]
    public async Task AdminValidationEndpoints_ShouldReturnProblemDetailsAcrossFinancialModules()
    {
        using var client = await CreateAuthenticatedClientAsync("AdminUser");

        var responses = new[]
        {
            await client.GetAsync("/api/v1/commerce?page=0"),
            await client.GetAsync("/api/v1/credit-card?status=invalido"),
            await client.GetAsync("/api/v1/loan?status=invalido"),
            await client.GetAsync("/api/v1/savings-account?type=invalido"),
            await client.GetAsync("/api/v1/users?pageSize=0")
        };

        foreach (var response in responses)
        {
            var body = await response.Content.ReadFromJsonAsync<JsonElement>();
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
            body.GetProperty("status").GetInt32().Should().Be(400);
            body.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
        }
    }

    [Fact]
    public async Task AdminFinancialAssignmentEndpoints_ShouldCompleteCardSecondaryAccountAndLoanFlows()
    {
        using var client = await CreateAuthenticatedClientAsync("AdminUser");
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var clientUser = await userManager.FindByNameAsync("ClientUser");
        clientUser.Should().NotBeNull();

        using var card = await client.PostAsJsonAsync("/api/v1/credit-card", new
        {
            clientId = clientUser!.Id,
            creditLimit = 5000m
        });
        card.StatusCode.Should().Be(HttpStatusCode.Created);

        using var account = await client.PostAsJsonAsync("/api/v1/savings-account", new
        {
            cedulaClient = clientUser.Cedula,
            initialBalance = 100m
        });
        account.StatusCode.Should().Be(HttpStatusCode.Created);

        using var loan = await client.PostAsJsonAsync("/api/v1/loan", new
        {
            clientId = clientUser.Id,
            amount = 1000m,
            annualRate = 12m,
            monthsInstallments = 6,
            confirmHighRisk = true
        });
        loan.StatusCode.Should().Be(HttpStatusCode.Created);
    }

    [Fact]
    public async Task AdminQueryAndUpdateEndpoints_ShouldBeCoveredThroughHttpPipeline()
    {
        using var client = await CreateAuthenticatedClientAsync("AdminUser");
        var isolatedClient = await SeedIsolatedClientAsync();

        var commerceRequest = new
        {
            name = $"HTTP Commerce {Guid.NewGuid():N}",
            description = "Commerce created for the administrative endpoint workflow",
            logo = "logo.svg",
            rnc = $"{Random.Shared.Next(100000000, 999999999)}",
            email = $"commerce-{Guid.NewGuid():N}@test.local",
            phoneNumber = "8095550102"
        };
        using var commerceCreate = await client.PostAsJsonAsync("/api/v1/commerce", commerceRequest);
        commerceCreate.StatusCode.Should().Be(HttpStatusCode.Created);
        var commerceBody = await commerceCreate.Content.ReadFromJsonAsync<JsonElement>();
        var commerceId = commerceBody.GetProperty("id").GetInt32();

        (await client.GetAsync("/api/v1/commerce?page=1&pageSize=20&status=todos"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/api/v1/commerce/{commerceId}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        using var commerceUpdate = await client.PutAsJsonAsync($"/api/v1/commerce/{commerceId}", new
        {
            name = commerceRequest.name + " Updated",
            description = commerceRequest.description,
            logo = commerceRequest.logo,
            rnc = commerceRequest.rnc,
            email = commerceRequest.email,
            phoneNumber = commerceRequest.phoneNumber
        });
        commerceUpdate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        var commerceUserName = $"commerce-endpoint-{Guid.NewGuid():N}";
        using var commerceUser = await client.PostAsJsonAsync($"/api/v1/users/commerce/{commerceId}", new
        {
            firstName = "Commerce",
            lastName = "Endpoint",
            cedula = $"402{Random.Shared.Next(100000000, 999999999)}",
            userName = commerceUserName,
            email = $"commerce-user-{Guid.NewGuid():N}@test.local",
            password = "123Pa$$word!",
            confirmPassword = "123Pa$$word!"
        });
        commerceUser.StatusCode.Should().Be(HttpStatusCode.Created);
        (await client.GetAsync("/api/v1/users/commerce?page=1&pageSize=20"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        using var commerceDeactivate = await client.PatchAsJsonAsync($"/api/v1/commerce/{commerceId}/status", new { status = false });
        commerceDeactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using (var commerceIdentityScope = _factory.Services.CreateScope())
        {
            var commerceUserManager = commerceIdentityScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var associatedUser = await commerceUserManager.FindByNameAsync(commerceUserName);
            associatedUser.Should().NotBeNull();
            associatedUser!.IsActive.Should().BeFalse();
        }

        using var commerceActivate = await client.PatchAsJsonAsync($"/api/v1/commerce/{commerceId}/status", new { status = true });
        commerceActivate.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using (var commerceIdentityScope = _factory.Services.CreateScope())
        {
            var commerceUserManager = commerceIdentityScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
            var associatedUser = await commerceUserManager.FindByNameAsync(commerceUserName);
            associatedUser.Should().NotBeNull();
            associatedUser!.IsActive.Should().BeFalse();
        }

        (await client.GetAsync("/api/v1/users?page=1&pageSize=20&role=Client"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/api/v1/users/{isolatedClient.Id}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);

        var createdUserName = $"admin-http-{Guid.NewGuid():N}";
        using var userCreate = await client.PostAsJsonAsync("/api/v1/users", new
        {
            firstName = "HTTP",
            lastName = "User",
            cedula = $"402{Random.Shared.Next(100000000, 999999999)}",
            userName = createdUserName,
            email = $"http-user-{Guid.NewGuid():N}@test.local",
            password = "123Pa$$word!",
            confirmPassword = "123Pa$$word!",
            role = "Client",
            initialAmount = 0m
        });
        userCreate.StatusCode.Should().Be(HttpStatusCode.Created);

        using var identityScope = _factory.Services.CreateScope();
        var userManager = identityScope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var createdUser = await userManager.FindByNameAsync(createdUserName);
        createdUser.Should().NotBeNull();

        using var userUpdate = await client.PutAsJsonAsync($"/api/v1/users/{createdUser!.Id}", new
        {
            firstName = "HTTP Updated",
            lastName = "User",
            cedula = createdUser.Cedula,
            email = createdUser.Email,
            userName = createdUser.UserName,
            password = (string?)null,
            confirmPassword = (string?)null,
            additionalAmount = 0m
        });
        userUpdate.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var userDeactivate = await client.PatchAsJsonAsync($"/api/v1/users/{createdUser.Id}/status", new { status = false });
        userDeactivate.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var userActivate = await client.PatchAsJsonAsync($"/api/v1/users/{createdUser.Id}/status", new { status = true });
        userActivate.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var cardCreate = await client.PostAsJsonAsync("/api/v1/credit-card", new
        {
            clientId = isolatedClient.Id,
            creditLimit = 5000m
        });
        cardCreate.StatusCode.Should().Be(HttpStatusCode.Created);
        var cardBody = await cardCreate.Content.ReadFromJsonAsync<JsonElement>();
        var cardId = cardBody.GetProperty("id").GetInt32();
        (await client.GetAsync("/api/v1/credit-card?page=1&pageSize=20&status=todas"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/api/v1/credit-card/{cardId}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        using var cardLimit = await client.PatchAsJsonAsync($"/api/v1/credit-card/{cardId}/limit", new { newLimit = 6000m });
        cardLimit.StatusCode.Should().Be(HttpStatusCode.NoContent);
        using var cardCancel = await client.PatchAsync($"/api/v1/credit-card/{cardId}/cancel", null);
        cardCancel.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var savingsCreate = await client.PostAsJsonAsync("/api/v1/savings-account", new
        {
            cedulaClient = isolatedClient.Cedula,
            initialBalance = 250m
        });
        savingsCreate.StatusCode.Should().Be(HttpStatusCode.Created);
        using var savingsScope = _factory.Services.CreateScope();
        var businessContext = savingsScope.ServiceProvider.GetRequiredService<ArtemisBankingDbContext>();
        var secondaryAccount = await businessContext.Savings
            .Where(account => account.UserId == isolatedClient.Id && account.Type == AccountType.Secondary)
            .OrderByDescending(account => account.CreatedAt)
            .FirstAsync();
        (await client.GetAsync("/api/v1/savings-account?page=1&pageSize=20&status=todas&type=todas"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/api/v1/savings-account/{secondaryAccount.AccountNumber}/transactions"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        using var savingsCancel = await client.PatchAsync($"/api/v1/savings-account/{secondaryAccount.AccountNumber}/cancel", null);
        savingsCancel.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var loanCreate = await client.PostAsJsonAsync("/api/v1/loan", new
        {
            clientId = isolatedClient.Id,
            amount = 500m,
            annualRate = 12m,
            monthsInstallments = 6,
            confirmHighRisk = true
        });
        loanCreate.StatusCode.Should().Be(HttpStatusCode.Created);
        var loanBody = await loanCreate.Content.ReadFromJsonAsync<JsonElement>();
        var loanId = loanBody.GetProperty("id").GetInt32();
        (await client.GetAsync("/api/v1/loan?page=1&pageSize=20&status=activos"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        (await client.GetAsync($"/api/v1/loan/{loanId}"))
            .StatusCode.Should().Be(HttpStatusCode.OK);
        using var loanRate = await client.PatchAsJsonAsync($"/api/v1/loan/{loanId}/rate", new { newRates = 15m });
        loanRate.StatusCode.Should().Be(HttpStatusCode.NoContent);
    }

    [Fact]
    public async Task HermesPay_ShouldSettleCardAccountAndRejectDuplicateIdempotencyKey()
    {
        var fixture = await SeedHermesPaymentDataAsync();
        using var client = await CreateAuthenticatedClientAsync("CommerceUser");
        var request = new
        {
            cardNumber = fixture.CardNumber,
            monthExpirationCard = fixture.ExpirationMonth,
            yearExpirationCard = fixture.ExpirationYear,
            cvc = "123",
            transactionAmount = 125m
        };

        using var first = new HttpRequestMessage(HttpMethod.Post,
            $"/api/v1/pay/process-payment/{fixture.CommerceId}")
        {
            Content = JsonContent.Create(request)
        };
        first.Headers.Add("Idempotency-Key", "hermes-integration-001");
        using var firstResponse = await client.SendAsync(first);

        firstResponse.StatusCode.Should().Be(HttpStatusCode.NoContent);

        using var second = new HttpRequestMessage(HttpMethod.Post,
            $"/api/v1/pay/process-payment/{fixture.CommerceId}")
        {
            Content = JsonContent.Create(request)
        };
        second.Headers.Add("Idempotency-Key", "hermes-integration-001");
        using var secondResponse = await client.SendAsync(second);
        var secondBody = await secondResponse.Content.ReadFromJsonAsync<JsonElement>();

        secondResponse.StatusCode.Should().Be(HttpStatusCode.BadRequest);
        secondResponse.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        secondBody.GetProperty("title").GetString().Should().Be("Pago rechazado");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ArtemisBankingDbContext>();
        (await context.CreditCards.SingleAsync(card => card.CardNumber == fixture.CardNumber))
            .AmountOwed.Should().Be(125);
        (await context.Savings.SingleAsync(account => account.AccountNumber == fixture.SettlementAccount))
            .Balance.Should().Be(125);
        (await context.Consumptions.CountAsync(consumption =>
            consumption.CreditCardId == fixture.CardId))
            .Should().Be(1);
        (await context.IdempotencyRecords.CountAsync(record => record.Key == "hermes-integration-001"))
            .Should().Be(1);
    }

    [Fact]
    public async Task CashierDeposit_ShouldChangeBalanceOnceForRepeatedIdempotencyKey()
    {
        var fixture = await GetClientPrimaryAccountAsync();
        using var client = await CreateAuthenticatedClientAsync("CashierUser");
        using var first = new HttpRequestMessage(HttpMethod.Post, "/api/v1/Transaction/deposit")
        {
            Content = JsonContent.Create(new
            {
                accountNumber = fixture.AccountNumber,
                amount = 50m
            })
        };
        first.Headers.Add("Idempotency-Key", "cashier-integration-001");
        using var firstResponse = await client.SendAsync(first);
        firstResponse.StatusCode.Should().Be(HttpStatusCode.OK);

        using var second = new HttpRequestMessage(HttpMethod.Post, "/api/v1/Transaction/deposit")
        {
            Content = JsonContent.Create(new
            {
                accountNumber = fixture.AccountNumber,
                amount = 50m
            })
        };
        second.Headers.Add("Idempotency-Key", "cashier-integration-001");
        using var secondResponse = await client.SendAsync(second);
        await AssertProblemDetailsAsync(secondResponse, HttpStatusCode.BadRequest, "Operación rechazada");

        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ArtemisBankingDbContext>();
        (await context.Savings.SingleAsync(account => account.AccountNumber == fixture.AccountNumber))
            .Balance.Should().Be(fixture.InitialBalance + 50);
        (await context.IdempotencyRecords.CountAsync(record => record.Key == "cashier-integration-001"))
            .Should().Be(1);
        (await context.Transactions.CountAsync(transaction =>
            transaction.SourceAccountNumber == "CASHIER" &&
            transaction.DestinationAccountNumber == fixture.AccountNumber))
            .Should().Be(1);
    }

    private HttpClient CreateClient()
        => _factory.CreateClient(new WebApplicationFactoryClientOptions
        {
            AllowAutoRedirect = false,
            BaseAddress = new Uri("https://localhost")
        });

    private async Task<HttpClient> CreateAuthenticatedClientAsync(string username)
    {
        var client = CreateClient();
        if (username == "CashierUser" || username == "ClientUser")
        {
            using var scope = _factory.Services.CreateScope();
            var userManager = scope.ServiceProvider.GetRequiredService<Microsoft.AspNetCore.Identity.UserManager<ABP.Infraestructure.identity.Entities.ApplicationUser>>();
            var user = await userManager.FindByNameAsync(username);
            var jwtService = scope.ServiceProvider.GetRequiredService<ABP.Core.Application.Interfaces.IServices.IJwtService>();
            var token = await jwtService.GenerateTokenAsync(user!.Id, user.UserName!, user.Email!, [user.Role.ToString()], user.CommerceId ?? 0);
            
            client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return client;
        }

        var login = await client.PostAsJsonAsync("/api/v1/Account/login", new
        {
            userName = username,
            password = "123Pa$$word!"
        });
        login.StatusCode.Should().Be(HttpStatusCode.OK);
        var body = await login.Content.ReadFromJsonAsync<JsonElement>();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(
            "Bearer", body.GetProperty("jwt").GetString());
        return client;
    }

    private static async Task AssertProblemDetailsAsync(
        HttpResponseMessage response,
        HttpStatusCode statusCode,
        string title)
    {
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        response.StatusCode.Should().Be(statusCode);
        response.Content.Headers.ContentType!.MediaType.Should().Be("application/problem+json");
        body.GetProperty("status").GetInt32().Should().Be((int)statusCode);
        body.GetProperty("title").GetString().Should().Be(title);
        body.GetProperty("traceId").GetString().Should().NotBeNullOrWhiteSpace();
    }

    private async Task<ClientAccountFixture> GetClientPrimaryAccountAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ArtemisBankingDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var client = await userManager.FindByNameAsync("ClientUser");
        client.Should().NotBeNull();
        var account = await context.Savings.SingleAsync(item =>
            item.UserId == client!.Id && item.Type == AccountType.Primary);
        return new ClientAccountFixture(account.AccountNumber, account.Balance);
    }

    private async Task<IsolatedClientFixture> SeedIsolatedClientAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var context = scope.ServiceProvider.GetRequiredService<ArtemisBankingDbContext>();
        var roleName = UserRole.Client.ToString();
        if (!await roleManager.RoleExistsAsync(roleName))
            (await roleManager.CreateAsync(new IdentityRole(roleName))).Succeeded.Should().BeTrue();

        var user = new ApplicationUser
        {
            UserName = $"admin-endpoint-client-{Guid.NewGuid():N}",
            Email = $"admin-endpoint-client-{Guid.NewGuid():N}@test.local",
            FirstName = "Endpoint",
            LastName = "Client",
            Cedula = $"402{Random.Shared.Next(100000000, 999999999)}",
            Role = UserRole.Client,
            IsActive = true,
            EmailConfirmed = true
        };
        (await userManager.CreateAsync(user, "123Pa$$word!")).Succeeded.Should().BeTrue();
        (await userManager.AddToRoleAsync(user, roleName)).Succeeded.Should().BeTrue();

        var account = new SavingsAccount
        {
            AccountNumber = $"8{Random.Shared.Next(100000000, 999999999)}",
            UserId = user.Id,
            Type = AccountType.Primary,
            Status = AccountStatus.Active,
            Balance = 10000m,
            CreatedAt = DateTime.UtcNow,
            CreatedByAdminId = "integration-test"
        };
        await context.Savings.AddAsync(account);
        await context.SaveChangesAsync();
        return new IsolatedClientFixture(user.Id, user.Cedula);
    }

    private async Task<HermesFixture> SeedHermesPaymentDataAsync()
    {
        using var scope = _factory.Services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<ArtemisBankingDbContext>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var commerce = await context.Commerces.SingleAsync(item => item.Name == "Default Commerce");

        var commerceUser = await userManager.FindByNameAsync("CommerceUser");
        if (commerceUser is null)
        {
            commerceUser = new ApplicationUser
            {
                UserName = "CommerceUser",
                Email = "integration-commerce@test.local",
                FirstName = "Integration",
                LastName = "Commerce",
                Cedula = "40209990001",
                Role = UserRole.Commerce,
                CommerceId = commerce.Id,
                IsActive = true,
                EmailConfirmed = true
            };
            (await userManager.CreateAsync(commerceUser, "123Pa$$word!")).Succeeded.Should().BeTrue();
            if (!await roleManager.RoleExistsAsync(UserRole.Commerce.ToString()))
                (await roleManager.CreateAsync(new IdentityRole(UserRole.Commerce.ToString()))).Succeeded.Should().BeTrue();
            (await userManager.AddToRoleAsync(commerceUser, UserRole.Commerce.ToString())).Succeeded.Should().BeTrue();
        }

        var settlementAccount = await context.Savings.SingleOrDefaultAsync(account =>
            account.UserId == commerceUser.Id && account.Type == AccountType.Primary);
        if (settlementAccount is null)
        {
            settlementAccount = new SavingsAccount
            {
                AccountNumber = "990000001",
                UserId = commerceUser.Id,
                Type = AccountType.Primary,
                Status = AccountStatus.Active,
                Balance = 0,
                CreatedAt = DateTime.UtcNow,
                CreatedByAdminId = "SYSTEM"
            };
            await context.Savings.AddAsync(settlementAccount);
        }

        var clientUser = await userManager.FindByNameAsync("ClientUser");
        clientUser.Should().NotBeNull();
        const string cardNumber = "4111111111111111";
        var card = await context.CreditCards.SingleOrDefaultAsync(item => item.CardNumber == cardNumber);
        var expiration = DateTime.UtcNow.AddYears(1);
        if (card is null)
        {
            card = new CreditCard
            {
                CardNumber = cardNumber,
                ClientId = clientUser!.Id,
                CreditLimit = 5000,
                AmountOwed = 0,
                CVCHash = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes("123"))),
                ExpirationDate = expiration.ToString("MM/yy"),
                Status = CardStatus.Active,
                CreatedAt = DateTime.UtcNow,
                AssignedByAdminId = "admin-seed"
            };
            await context.CreditCards.AddAsync(card);
        }

        await context.SaveChangesAsync();
        return new HermesFixture(
            commerce.Id,
            card.Id,
            card.CardNumber,
            settlementAccount.AccountNumber,
            expiration.ToString("MM"),
            expiration.ToString("yyyy"));
    }

    private sealed record HermesFixture(
        int CommerceId,
        int CardId,
        string CardNumber,
        string SettlementAccount,
        string ExpirationMonth,
        string ExpirationYear);

    private sealed record ClientAccountFixture(string AccountNumber, decimal InitialBalance);

    private sealed record IsolatedClientFixture(string Id, string Cedula);
}

public sealed class ApiApplicationFactory : WebApplicationFactory<Program>
{
    protected override void ConfigureWebHost(IWebHostBuilder builder)
    {
        builder.UseEnvironment("Testing");
        builder.UseSetting("UseInMemoryDatabase", "true");
        builder.ConfigureAppConfiguration((_, configuration) =>
        {
            configuration.AddInMemoryCollection(new Dictionary<string, string?>
            {
                ["UseInMemoryDatabase"] = "true",
                ["JWT:Key"] = "integration-test-key-with-at-least-32-characters",
                ["JWT:Issuer"] = "ArtemisBank.API",
                ["JWT:Audience"] = "ArtemisBank.Client",
                ["JWT:DurationInMinutes"] = "30",
                ["EmailSettings:EnableSsl"] = "false"
            });
        });
        builder.ConfigureServices(services =>
        {
            // Reemplazar los contextos explícitamente evita que una configuración
            // externa de la API pueda hacer que estas pruebas contacten Supabase.
            services.RemoveAll<ArtemisBankingDbContext>();
            services.RemoveAll<DbContextOptions<ArtemisBankingDbContext>>();
            services.AddDbContext<ArtemisBankingDbContext>(options =>
                options.UseInMemoryDatabase("ApiIntegrationBusiness")
                    .ConfigureWarnings(warnings =>
                        warnings.Ignore(InMemoryEventId.TransactionIgnoredWarning)));

            services.RemoveAll<IdentityContext>();
            services.RemoveAll<DbContextOptions<IdentityContext>>();
            services.AddDbContext<IdentityContext>(options =>
                options.UseInMemoryDatabase("ApiIntegrationIdentity"));

            services.RemoveAll<ICorreoServices>();
            services.RemoveAll<IEmailServices>();
            var email = new NoOpEmailService();
            services.AddSingleton<ICorreoServices>(email);
            services.AddSingleton<IEmailServices>(email);
        });
    }

    private sealed class NoOpEmailService : ICorreoServices
    {
        public Task SendAsync(string to, string subject, string body) => Task.CompletedTask;

        public Task SendEmailAsync(EmailRequest request) => Task.CompletedTask;
    }
}
