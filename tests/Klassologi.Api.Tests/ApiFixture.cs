using System.Net.Http.Headers;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;
using MySqlConnector;
using Testcontainers.MySql;

[assembly: AssemblyFixture(typeof(Klassologi.Api.Tests.ApiFixture))]

namespace Klassologi.Api.Tests;

/// <summary>
/// Startar en egen MySQL-container med samma serverinställningar som compose.yaml och kör API:t mot den.
/// Delas av alla tester. Varje test skapar egna användare och klasser med nya id:n, så testerna påverkar inte varandra.
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
        Factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b => b
            .UseSetting("ConnectionStrings:Klassologi", ConnectionString)
            .ConfigureTestServices(services => services
                .AddAuthentication(TestAuthHandler.SchemeName)
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null)));
        Factory.CreateClient(); // Startar API:t så att migreringarna körs.
    }

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _mysql.DisposeAsync();
    }

    /// <summary>En klient utan inloggning.</summary>
    public HttpClient Anonymous() => Factory.CreateClient();

    /// <summary>En klient inloggad som den angivna användaren.</summary>
    public HttpClient As(Guid userId)
    {
        var client = Factory.CreateClient();
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, userId.ToString());
        return client;
    }

    /// <summary>Skapar en användare direkt i databasen. Kontohantering kommer i steg 3.</summary>
    public async Task<Guid> CreateUserAsync(string displayName = "Testanvändare", bool isAdministrator = false)
    {
        var id = Guid.CreateVersion7();
        await using var connection = new MySqlConnection(ConnectionString);
        await connection.ExecuteAsync(
            "INSERT INTO users (Id, DisplayName, IsAdministrator) VALUES (@id, @displayName, @isAdministrator)",
            new { id, displayName, isAdministrator });
        return id;
    }

    public async Task<MySqlConnection> OpenAsync()
    {
        var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync();
        return connection;
    }
}
