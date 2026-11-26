using System.Net;
using System.Net.Http.Json;
using Dapper;

namespace Klassologi.Api.Tests;

public sealed class StatusTests(ApiFixture api)
{
    [Fact]
    public async Task Status_reports_ok_after_migrations_have_run()
    {
        var response = await api.Anonymous().GetAsync("/api/status", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<StatusBody>(TestContext.Current.CancellationToken);
        Assert.Equal("ok", body?.Status);
        Assert.True(body?.Migrations >= 1);
    }

    [Fact]
    public async Task Migrations_set_swedish_collation_as_database_default()
    {
        // Testcontainern har utf8mb4_0900_ai_ci som serverstandard, så resultatet visar att migreringen har verkan.
        await using var connection = await api.OpenAsync();
        var defaults = await connection.QuerySingleAsync<(string Charset, string Collation)>(
            """
            SELECT DEFAULT_CHARACTER_SET_NAME, DEFAULT_COLLATION_NAME
            FROM information_schema.SCHEMATA
            WHERE SCHEMA_NAME = DATABASE()
            """);

        Assert.Equal("utf8mb4", defaults.Charset);
        Assert.Equal("utf8mb4_sv_0900_ai_ci", defaults.Collation);
    }

    sealed record StatusBody(string Status, int Migrations);
}
