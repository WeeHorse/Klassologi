using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using Dapper;
using Klassologi.Api.Data;
using Klassologi.Api.Features.Invitations;

namespace Klassologi.Api.Tests;

/// <summary>UC-02 Bjuda in (enkel variant), UC-04 Aktivera konto via inbjudan och första administratören (Q15).</summary>
public sealed class InvitationTests(ApiFixture api)
{
    static CancellationToken Ct => TestContext.Current.CancellationToken;

    const string Password = "ett-långt-lösenord";

    static Task<HttpResponseMessage> AcceptNewAsync(HttpClient client, string token, string email, string password = Password, string displayName = "Ny Användare") =>
        client.PostAsJsonAsync("/api/invitations/accept", new { token, displayName, email, password }, Ct);

    static string NewEmail() => $"user-{Guid.NewGuid():N}@example.se";

    async Task<string?> RoleOfAsync(Guid classId, Guid userId)
    {
        await using var connection = await api.OpenAsync();
        return await connection.ExecuteScalarAsync<string?>(
            "SELECT Role FROM class_memberships WHERE ClassId = @classId AND UserId = @userId", new { classId, userId });
    }

    async Task<bool> IsUsedAsync(string token)
    {
        await using var connection = await api.OpenAsync();
        return await connection.ExecuteScalarAsync<bool>(
            "SELECT UsedAt IS NOT NULL FROM invitations WHERE TokenHash = @hash", new { hash = SHA256.HashData(Encoding.UTF8.GetBytes(token)) });
    }

    static async Task<string?> CodeAsync(HttpResponseMessage response)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return json.RootElement.TryGetProperty("code", out var c) ? c.GetString() : null;
    }

    // ---------- UC-02 ----------

    [Fact]
    public async Task Teacher_creates_invitation_valid_for_seven_days()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();

        var response = await api.As(teacher).PostAsJsonAsync($"/api/classes/{classId}/invitations", new { role = "Student" }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var link = await response.Content.ReadFromJsonAsync<ApiFixture.Link>(Ct);
        Assert.StartsWith("/aktivera#", link!.Url);
        Assert.InRange(link.ExpiresAt, DateTime.UtcNow.AddDays(7).AddMinutes(-1), DateTime.UtcNow.AddDays(7).AddMinutes(1));
    }

    [Fact]
    public async Task Only_a_hash_of_the_token_is_stored()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var token = await api.InviteAsync(classId, teacher, "Student");

        await using var connection = await api.OpenAsync();
        var stored = await connection.QuerySingleAsync<byte[]>(
            "SELECT TokenHash FROM invitations WHERE TokenHash = @hash", new { hash = SHA256.HashData(Encoding.UTF8.GetBytes(token)) });
        Assert.Equal(32, stored.Length);
        Assert.True(Base64UrlDecodedLength(token) >= 32, "Token ska ha minst 256 bitar slump.");
    }

    static int Base64UrlDecodedLength(string token) => token.Length * 3 / 4;

    [Theory]
    [InlineData("Student")]
    [InlineData("StudentRepresentative")]
    public async Task Member_who_is_not_teacher_cannot_invite(string role)
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var member = await api.CreateUserAsync();
        await api.As(teacher).PutAsJsonAsync($"/api/classes/{classId}/members/{member}", new { role }, Ct);

        var response = await api.As(member).PostAsJsonAsync($"/api/classes/{classId}/invitations", new { role = "Student" }, Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_of_another_class_cannot_invite()
    {
        var (classId, _) = await api.CreateClassWithTeacherAsync();
        var (_, otherTeacher) = await api.CreateClassWithTeacherAsync();

        var response = await api.As(otherTeacher).PostAsJsonAsync($"/api/classes/{classId}/invitations", new { role = "Student" }, Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Invitation_to_unknown_class_gives_404()
    {
        var admin = await api.CreateUserAsync(isAdministrator: true);

        var response = await api.As(admin).PostAsJsonAsync($"/api/classes/{Guid.CreateVersion7()}/invitations", new { role = "Student" }, Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_cannot_invite()
    {
        var (classId, _) = await api.CreateClassWithTeacherAsync();

        var response = await api.Anonymous().PostAsJsonAsync($"/api/classes/{classId}/invitations", new { role = "Student" }, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Invitation_with_invalid_role_gives_400()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();

        var response = await api.As(teacher).PostAsJsonAsync($"/api/classes/{classId}/invitations", new { role = "Rektor" }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    // ---------- UC-04 ----------

    [Fact]
    public async Task Inspect_shows_class_and_role()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync("Åk 9 – Ängby");
        var token = await api.InviteAsync(classId, teacher, "StudentRepresentative");

        var response = await api.Anonymous().PostAsJsonAsync("/api/invitations/inspect", new { token }, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        Assert.Equal("Åk 9 – Ängby", json.RootElement.GetProperty("className").GetString());
        Assert.Equal("StudentRepresentative", json.RootElement.GetProperty("role").GetString());
        Assert.False(json.RootElement.GetProperty("isAdministrator").GetBoolean());
    }

    [Fact]
    public async Task New_account_is_created_logged_in_and_made_member()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var token = await api.InviteAsync(classId, teacher, "Student");
        var browser = api.Browser();
        var email = NewEmail();

        var response = await AcceptNewAsync(browser, token, email, displayName: "  Åsa Öberg  ");

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var me = await browser.GetFromJsonAsync<ApiFixture.Me>("/api/auth/me", Ct);
        Assert.Equal("Åsa Öberg", me!.DisplayName);
        Assert.Equal(email, me.Email);
        Assert.False(me.IsAdministrator);
        Assert.Equal("Student", await RoleOfAsync(classId, me.Id));
        Assert.True(await IsUsedAsync(token));
    }

    [Fact]
    public async Task Invitation_can_only_be_used_once()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var token = await api.InviteAsync(classId, teacher, "Student");
        await AcceptNewAsync(api.Browser(), token, NewEmail());

        var second = await AcceptNewAsync(api.Browser(), token, NewEmail());

        Assert.Equal(HttpStatusCode.NotFound, second.StatusCode);
    }

    [Fact]
    public async Task Used_expired_and_unknown_links_give_the_same_answer()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var used = await api.InviteAsync(classId, teacher, "Student");
        await AcceptNewAsync(api.Browser(), used, NewEmail());
        var expired = await api.InviteAsync(classId, teacher, "Student");
        await using (var connection = await api.OpenAsync())
            await connection.ExecuteAsync(
                "UPDATE invitations SET ExpiresAt = UTC_TIMESTAMP(6) - INTERVAL 1 SECOND WHERE TokenHash = @hash",
                new { hash = SHA256.HashData(Encoding.UTF8.GetBytes(expired)) });

        var bodies = new List<string>();
        foreach (var token in new[] { used, expired, "okänd-token" })
        {
            var inspect = await api.Anonymous().PostAsJsonAsync("/api/invitations/inspect", new { token }, Ct);
            var accept = await AcceptNewAsync(api.Browser(), token, NewEmail());
            Assert.Equal(HttpStatusCode.NotFound, inspect.StatusCode);
            Assert.Equal(HttpStatusCode.NotFound, accept.StatusCode);
            using var json = JsonDocument.Parse(await accept.Content.ReadAsStringAsync(Ct));
            bodies.Add(json.RootElement.GetProperty("title").GetString()!);
        }
        Assert.Single(bodies.Distinct());
    }

    [Fact]
    public async Task Logged_in_user_joins_class_with_existing_account()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var existing = await api.CreateUserAsync();
        var token = await api.InviteAsync(classId, teacher, "StudentRepresentative");

        var response = await api.As(existing).PostAsJsonAsync("/api/invitations/accept", new { token }, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("StudentRepresentative", await RoleOfAsync(classId, existing));
        Assert.True(await IsUsedAsync(token));
    }

    [Fact]
    public async Task Existing_member_keeps_role_but_invitation_is_consumed()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var token = await api.InviteAsync(classId, teacher, "Student");

        var response = await api.As(teacher).PostAsJsonAsync("/api/invitations/accept", new { token }, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal("Teacher", await RoleOfAsync(classId, teacher));
        Assert.True(await IsUsedAsync(token));
    }

    [Fact]
    public async Task Email_that_already_has_account_gives_409_and_keeps_invitation()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var email = NewEmail();
        await api.CreateAccountAsync(classId, teacher, email);
        var token = await api.InviteAsync(classId, teacher, "Student");

        var response = await AcceptNewAsync(api.Browser(), token, email.ToUpperInvariant());

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.Equal("email_taken", await CodeAsync(response));
        Assert.False(await IsUsedAsync(token));
    }

    [Theory]
    [InlineData("Namn", "ogiltig-epost", Password, "email")]
    [InlineData("", "ok@example.se", Password, "displayName")]
    [InlineData("   ", "ok@example.se", Password, "displayName")]
    [InlineData("Namn", "ok@example.se", "elva-tecken", "password")]
    public async Task Invalid_fields_give_400_and_keep_invitation(string displayName, string email, string password, string field)
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var token = await api.InviteAsync(classId, teacher, "Student");
        email = email.Replace("ok@", $"ok-{Guid.NewGuid():N}@");

        var response = await AcceptNewAsync(api.Browser(), token, email, password, displayName);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        Assert.True(json.RootElement.GetProperty("errors").TryGetProperty(field, out _), $"Fel saknas för {field}.");
        Assert.False(await IsUsedAsync(token));
    }

    [Theory]
    [InlineData(11, HttpStatusCode.BadRequest)]
    [InlineData(12, HttpStatusCode.Created)]
    [InlineData(128, HttpStatusCode.Created)]
    [InlineData(129, HttpStatusCode.BadRequest)]
    public async Task Password_must_be_12_to_128_characters_without_character_class_rules(int length, HttpStatusCode expected)
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var token = await api.InviteAsync(classId, teacher, "Student");

        var response = await AcceptNewAsync(api.Browser(), token, NewEmail(), new string('a', length));

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Display_name_may_be_at_most_100_characters()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var ok = await api.InviteAsync(classId, teacher, "Student");
        var tooLong = await api.InviteAsync(classId, teacher, "Student");

        Assert.Equal(HttpStatusCode.Created, (await AcceptNewAsync(api.Browser(), ok, NewEmail(), displayName: new string('Ö', 100))).StatusCode);
        Assert.Equal(HttpStatusCode.BadRequest, (await AcceptNewAsync(api.Browser(), tooLong, NewEmail(), displayName: new string('Ö', 101))).StatusCode);
    }

    [Fact]
    public async Task Concurrent_use_of_the_same_invitation_succeeds_only_once()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var token = await api.InviteAsync(classId, teacher, "Student");

        var responses = await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => AcceptNewAsync(api.Browser(), token, NewEmail())));

        Assert.Single(responses, r => r.StatusCode == HttpStatusCode.Created);
        Assert.All(responses.Where(r => r.StatusCode != HttpStatusCode.Created), r => Assert.Equal(HttpStatusCode.NotFound, r.StatusCode));
        await using var connection = await api.OpenAsync();
        Assert.Equal(1, await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM class_memberships WHERE ClassId = @classId AND Role = 'Student'", new { classId }));
    }

    // ---------- Första administratören (Q15) ----------

    [Fact]
    public async Task Admin_invitation_command_creates_link_valid_for_24_hours()
    {
        var link = await Invitations.CreateAdministratorInvitationAsync(new Db(api.ConnectionString));

        Assert.StartsWith("/aktivera#", link.Url);
        Assert.InRange(link.ExpiresAt, DateTime.UtcNow.AddHours(24).AddMinutes(-1), DateTime.UtcNow.AddHours(24).AddMinutes(1));
    }

    [Fact]
    public async Task Admin_invitation_creates_administrator_account()
    {
        var link = await Invitations.CreateAdministratorInvitationAsync(new Db(api.ConnectionString));
        var browser = api.Browser();

        var inspect = await api.Anonymous().PostAsJsonAsync("/api/invitations/inspect", new { token = ApiFixture.TokenFrom(link.Url) }, Ct);
        var response = await AcceptNewAsync(browser, ApiFixture.TokenFrom(link.Url), NewEmail());

        using (var json = JsonDocument.Parse(await inspect.Content.ReadAsStringAsync(Ct)))
            Assert.True(json.RootElement.GetProperty("isAdministrator").GetBoolean());
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        Assert.True((await browser.GetFromJsonAsync<ApiFixture.Me>("/api/auth/me", Ct))!.IsAdministrator);
        Assert.Equal(HttpStatusCode.Created, (await browser.PostAsJsonAsync("/api/classes", new { name = "Ny klass" }, Ct)).StatusCode);
    }

    [Fact]
    public async Task Logged_in_user_becomes_administrator_with_admin_invitation()
    {
        var existing = await api.CreateUserAsync();
        var link = await Invitations.CreateAdministratorInvitationAsync(new Db(api.ConnectionString));

        var response = await api.As(existing).PostAsJsonAsync("/api/invitations/accept", new { token = ApiFixture.TokenFrom(link.Url) }, Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(HttpStatusCode.Created, (await api.As(existing).PostAsJsonAsync("/api/classes", new { name = "Ny klass" }, Ct)).StatusCode);
    }
}
