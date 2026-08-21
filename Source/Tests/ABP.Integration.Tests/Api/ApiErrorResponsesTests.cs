using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using System.Net.Http.Json;
using Xunit;

namespace ABP.Integration.Tests.Api;

public class ApiErrorResponsesTests : IClassFixture<WebApplicationFactory<Program>>
{
    private readonly HttpClient _client;

    public ApiErrorResponsesTests(WebApplicationFactory<Program> factory)
    {
        _client = factory.CreateClient();
    }

    [Fact]
    public async Task GetResetToken_WhenUserNotFound_ShouldReturn400()
    {
        var response = await _client.PostAsJsonAsync("/api/v1/Account/get-reset-token", new { userName = "nonexistentuser123" });
        
        response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
    }
}
