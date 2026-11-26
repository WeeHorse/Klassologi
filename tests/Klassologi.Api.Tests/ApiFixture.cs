using System.Net.Http.Headers;
using System.Net.Http.Json;
using Dapper;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Identity;
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
    const string TestOrCookie = "TestOrCookie";

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
        Factory = CreateFactory(rateLimitPerMinute: 100_000);
        Factory.CreateClient(); // Startar API:t så att migreringarna körs.
    }

    /// <summary>
    /// Kör API:t mot testdatabasen. Anrop med "Authorization: Test &lt;id&gt;" loggas in via testschemat,
    /// övriga via den riktiga sessionscookien.
    /// </summary>
    public WebApplicationFactory<Program> CreateFactory(int rateLimitPerMinute) =>
        new WebApplicationFactory<Program>().WithWebHostBuilder(b => b
            .UseSetting("ConnectionStrings:Klassologi", ConnectionString)
            .UseSetting("Auth:RateLimitPerMinute", rateLimitPerMinute.ToString())
            .ConfigureTestServices(services => services
                .AddAuthentication(o =>
                {
                    o.DefaultScheme = TestOrCookie;
                    o.DefaultChallengeScheme = TestOrCookie;
                })
                .AddScheme<AuthenticationSchemeOptions, TestAuthHandler>(TestAuthHandler.SchemeName, null)
                .AddPolicyScheme(TestOrCookie, null, o => o.ForwardDefaultSelector = ctx =>
                    ctx.Request.Headers.Authorization.ToString().StartsWith(TestAuthHandler.SchemeName + " ", StringComparison.Ordinal)
                        ? TestAuthHandler.SchemeName
                        : IdentityConstants.ApplicationScheme)));

    public async ValueTask DisposeAsync()
    {
        await Factory.DisposeAsync();
        await _mysql.DisposeAsync();
    }

    static readonly WebApplicationFactoryClientOptions Https = new() { BaseAddress = new Uri("https://localhost") };

    /// <summary>En klient utan inloggning som sparar cookies, som en webbläsare.</summary>
    public HttpClient Browser() => Factory.CreateClient(Https);

    /// <summary>En klient utan inloggning.</summary>
    public HttpClient Anonymous() => Factory.CreateClient(Https);

    /// <summary>En klient inloggad som den angivna användaren via testschemat.</summary>
    public HttpClient As(Guid userId)
    {
        var client = Factory.CreateClient(Https);
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue(TestAuthHandler.SchemeName, userId.ToString());
        return client;
    }

    /// <summary>Skapar en användare utan inloggningsuppgifter direkt i databasen.</summary>
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

    public static string TokenFrom(string url) => url[(url.IndexOf('#') + 1)..];

    /// <summary>Skapar en klass via API:t och en lärare som har tagit emot lärarinbjudan.</summary>
    public async Task<(Guid ClassId, Guid TeacherId)> CreateClassWithTeacherAsync(string name = "9A")
    {
        var admin = await CreateUserAsync("Admin", isAdministrator: true);
        var response = await As(admin).PostAsJsonAsync("/api/classes", new { name });
        response.EnsureSuccessStatusCode();
        var created = await response.Content.ReadFromJsonAsync<CreatedClass>();
        var teacher = await CreateUserAsync("Lärare");
        (await As(teacher).PostAsJsonAsync("/api/invitations/accept", new { token = TokenFrom(created!.TeacherInvitation.Url) }))
            .EnsureSuccessStatusCode();
        return (created.Id, teacher);
    }

    /// <summary>Skapar en inbjudan till klassen med rollen, som läraren.</summary>
    public async Task<string> InviteAsync(Guid classId, Guid teacherId, string role)
    {
        var response = await As(teacherId).PostAsJsonAsync($"/api/classes/{classId}/invitations", new { role });
        response.EnsureSuccessStatusCode();
        return TokenFrom((await response.Content.ReadFromJsonAsync<Link>())!.Url);
    }

    /// <summary>Skapar ett nytt konto via en inbjudan. Returnerar en inloggad klient och användarens id.</summary>
    public async Task<(HttpClient Client, Guid UserId)> CreateAccountAsync(
        Guid classId, Guid teacherId, string email, string password = "ett-långt-lösenord", string role = "Student")
    {
        var token = await InviteAsync(classId, teacherId, role);
        var client = Browser();
        var response = await client.PostAsJsonAsync("/api/invitations/accept", new { token, displayName = "Ny användare", email, password });
        Assert.Equal(System.Net.HttpStatusCode.Created, response.StatusCode);
        var me = await client.GetFromJsonAsync<Me>("/api/auth/me");
        return (client, me!.Id);
    }

    public sealed record Link(string Url, DateTime ExpiresAt);
    public sealed record CreatedClass(Guid Id, string Name, Link TeacherInvitation);
    public sealed record Me(Guid Id, string DisplayName, string? Email, bool IsAdministrator);
}
