using System.Net;
using System.Net.Http.Json;
using Dapper;
using MySqlConnector;

namespace Klassologi.Api.Tests;

public sealed class StatusTests(ApiFixture api) : IClassFixture<ApiFixture>
{
    [Fact]
    public async Task Status_reports_ok_after_migrations_have_run()
    {
        var client = api.Factory.CreateClient();

        var response = await client.GetAsync("/api/status", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<StatusBody>(TestContext.Current.CancellationToken);
        Assert.Equal("ok", body?.Status);
        Assert.True(body?.Migrations >= 1);
    }

    [Fact]
    public async Task Database_uses_utf8mb4_and_explicit_collation()
    {
        api.Factory.CreateClient(); // Startar API:t, som kör migreringarna.

        await using var connection = new MySqlConnection(api.ConnectionString);
        var defaults = await connection.QuerySingleAsync<(string Charset, string Collation)>(
            """
            SELECT DEFAULT_CHARACTER_SET_NAME, DEFAULT_COLLATION_NAME
            FROM information_schema.SCHEMATA
            WHERE SCHEMA_NAME = DATABASE()
            """);

        Assert.Equal("utf8mb4", defaults.Charset);
        Assert.Equal("utf8mb4_0900_ai_ci", defaults.Collation);
    }

    sealed record StatusBody(string Status, int Migrations);
}
