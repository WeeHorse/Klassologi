using Microsoft.AspNetCore.Mvc.Testing;
using Testcontainers.MySql;

namespace Klassologi.Api.Tests;

/// <summary>
/// Startar en egen MySQL-container med samma inställningar som compose.yaml och kör API:t mot den.
/// Påverkar inte den lokala utvecklingsdatabasen.
/// </summary>
public sealed class ApiFixture : IAsyncLifetime
{
    readonly MySqlContainer _mysql = new MySqlBuilder("mysql:9.7")
        .WithCommand(
            "--character-set-server=utf8mb4",
            "--collation-server=utf8mb4_0900_ai_ci",
            "--sql-mode=STRICT_TRANS_TABLES,ONLY_FULL_GROUP_BY,NO_ZERO_IN_DATE,NO_ZERO_DATE,ERROR_FOR_DIVISION_BY_ZERO,NO_ENGINE_SUBSTITUTION",
            "--default-time-zone=+00:00")
        .Build();

    public WebApplicationFactory<Program> Factory { get; private set; } = null!;

    public string ConnectionString => _mysql.GetConnectionString();

    public async ValueTask InitializeAsync()
    {
        await _mysql.StartAsync();
        Factory = new WebApplicationFactory<Program>()
            .WithWebHostBuilder(b => b.UseSetting("ConnectionStrings:Klassologi", ConnectionString));
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _mysql.DisposeAsync();
    }
}
