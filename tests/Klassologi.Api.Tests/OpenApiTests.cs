using System.Net;
using System.Text.Json;
using Microsoft.AspNetCore.Hosting;

namespace Klassologi.Api.Tests;

/// <summary>API-dokumentationen finns under utveckling men exponeras inte i drift.</summary>
public sealed class OpenApiTests(ApiFixture api)
{
    static CancellationToken Ct => TestContext.Current.CancellationToken;

    [Fact]
    public async Task Development_exposes_openapi_document_with_the_api()
    {
        var response = await api.Anonymous().GetAsync("/openapi/v1.json", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        var paths = json.RootElement.GetProperty("paths");
        Assert.True(paths.TryGetProperty("/api/auth/login", out _));
        Assert.True(paths.TryGetProperty("/api/invitations/accept", out _));
    }

    [Theory]
    [InlineData("/openapi/v1.json")]
    [InlineData("/scalar")]
    public async Task Production_does_not_expose_api_documentation(string path)
    {
        await using var production = api.Factory.WithWebHostBuilder(b => b.UseEnvironment("Production"));
        var client = production.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        var response = await client.GetAsync(path, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }
}
