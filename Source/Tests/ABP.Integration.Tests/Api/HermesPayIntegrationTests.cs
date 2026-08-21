using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading.Tasks;
using Xunit;

namespace ABP.Integration.Tests.Api;

public class HermesPayIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public HermesPayIntegrationTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetTransactions_WithoutCommerceIdClaim_ShouldReturnForbidden()
    {
        // Try to access the endpoint without valid commerce claims
        // Since we don't have token generation in the test right away, we can send a request with NO token, which returns 401
        // But the requirement says "claim faltante -> 403". We need to mock JWT auth or bypass it, or create a valid JWT without the claim.
        // I will write this out later. Let's see what else I need.
    }
}
