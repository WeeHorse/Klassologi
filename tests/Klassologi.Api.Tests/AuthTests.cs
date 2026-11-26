using System.Net;
using System.Net.Http.Json;
using System.Text.Json;

namespace Klassologi.Api.Tests;

/// <summary>UC-05 Logga in och logga ut, samt gemensamma regler för session, spärr och anropstakt (steg 3).</summary>
public sealed class AuthTests(ApiFixture api)
{
    static CancellationToken Ct => TestContext.Current.CancellationToken;

    static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password }, Ct);

    static async Task<(string? Title, string? Code)> ProblemAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        var root = json.RootElement;
        return (root.TryGetProperty("title", out var t) ? t.GetString() : null,
                root.TryGetProperty("code", out var c) ? c.GetString() : null);
    }

    async Task<(string Email, string Password)> AccountAsync()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var email = $"elev-{Guid.NewGuid():N}@example.se";
        await api.CreateAccountAsync(classId, teacher, email, "ett-långt-lösenord");
        return (email, "ett-långt-lösenord");
    }

    [Fact]
    public async Task Login_with_correct_credentials_starts_session()
    {
        var (email, password) = await AccountAsync();
        var browser = api.Browser();

        var response = await LoginAsync(browser, email, password);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        var me = await browser.GetFromJsonAsync<ApiFixture.Me>("/api/auth/me", Ct);
        Assert.Equal(email, me!.Email);
    }

    [Fact]
    public async Task Session_cookie_is_httponly_secure_and_samesite_strict()
    {
        var (email, password) = await AccountAsync();

        var response = await LoginAsync(api.Browser(), email, password);

        var cookie = Assert.Single(response.Headers.GetValues("Set-Cookie"), c => c.StartsWith("klassologi.session="));
        Assert.Contains("httponly", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("secure", cookie, StringComparison.OrdinalIgnoreCase);
        Assert.Contains("samesite=strict", cookie, StringComparison.OrdinalIgnoreCase);
    }

    [Fact]
    public async Task Email_is_case_insensitive()
    {
        var (email, password) = await AccountAsync();

        var response = await LoginAsync(api.Browser(), email.ToUpperInvariant(), password);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Wrong_password_and_unknown_email_give_the_same_answer()
    {
        var (email, _) = await AccountAsync();

        var wrongPassword = await LoginAsync(api.Browser(), email, "fel-lösenord-som-är-långt");
        var unknownEmail = await LoginAsync(api.Browser(), $"okand-{Guid.NewGuid():N}@example.se", "fel-lösenord-som-är-långt");

        Assert.Equal(HttpStatusCode.Unauthorized, wrongPassword.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownEmail.StatusCode);
        Assert.Equal(await ProblemAsync(wrongPassword), await ProblemAsync(unknownEmail));
        Assert.Equal("invalid_credentials", (await ProblemAsync(wrongPassword)).Code);
    }

    [Fact]
    public async Task Five_failed_attempts_lock_the_account_even_for_the_correct_password()
    {
        var (email, password) = await AccountAsync();
        for (var i = 0; i < 5; i++)
            await LoginAsync(api.Browser(), email, "fel-lösenord-som-är-långt");

        var response = await LoginAsync(api.Browser(), email, password);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal("locked_out", (await ProblemAsync(response)).Code);
    }

    [Fact]
    public async Task Four_failed_attempts_do_not_lock_the_account()
    {
        var (email, password) = await AccountAsync();
        for (var i = 0; i < 4; i++)
            await LoginAsync(api.Browser(), email, "fel-lösenord-som-är-långt");

        var response = await LoginAsync(api.Browser(), email, password);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Me_requires_login()
    {
        var response = await api.Anonymous().GetAsync("/api/auth/me", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Logout_ends_all_sessions_for_the_account()
    {
        var (email, password) = await AccountAsync();
        var first = api.Browser();
        var second = api.Browser();
        await LoginAsync(first, email, password);
        await LoginAsync(second, email, password);

        var logout = await first.PostAsync("/api/auth/logout", null, Ct);

        Assert.Equal(HttpStatusCode.NoContent, logout.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await first.GetAsync("/api/auth/me", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await second.GetAsync("/api/auth/me", Ct)).StatusCode);
    }

    [Fact]
    public async Task Api_answers_401_instead_of_redirecting_to_a_login_page()
    {
        var response = await api.Browser().GetAsync("/api/classes", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Null(response.Headers.Location);
    }

    [Fact]
    public async Task Too_many_login_attempts_from_one_address_give_429()
    {
        await using var factory = api.CreateFactory(rateLimitPerMinute: 3);
        var client = factory.CreateClient(new() { BaseAddress = new Uri("https://localhost") });

        var statuses = new List<HttpStatusCode>();
        for (var i = 0; i < 4; i++)
            statuses.Add((await LoginAsync(client, "x@example.se", "fel-lösenord-som-är-långt")).StatusCode);

        Assert.Equal([HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.Unauthorized, HttpStatusCode.TooManyRequests], statuses);
    }
}
