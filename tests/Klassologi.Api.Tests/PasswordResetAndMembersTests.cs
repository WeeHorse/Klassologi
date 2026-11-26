using System.Net;
using System.Net.Http.Json;
using System.Security.Cryptography;
using System.Text;
using Dapper;

namespace Klassologi.Api.Tests;

/// <summary>Återställa lösenord och medlemslistan för lärare och administratörer (steg 3).</summary>
public sealed class PasswordResetAndMembersTests(ApiFixture api)
{
    static CancellationToken Ct => TestContext.Current.CancellationToken;

    const string OldPassword = "ett-långt-lösenord";
    const string NewPassword = "ett-helt-nytt-lösenord";

    static string NewEmail() => $"user-{Guid.NewGuid():N}@example.se";

    static Task<HttpResponseMessage> CreateResetAsync(HttpClient actor, Guid classId, Guid userId) =>
        actor.PostAsync($"/api/classes/{classId}/members/{userId}/password-reset", null, Ct);

    static Task<HttpResponseMessage> CompleteAsync(HttpClient client, string token, string newPassword = NewPassword) =>
        client.PostAsJsonAsync("/api/password-resets/complete", new { token, newPassword }, Ct);

    static Task<HttpResponseMessage> LoginAsync(HttpClient client, string email, string password) =>
        client.PostAsJsonAsync("/api/auth/login", new { email, password }, Ct);

    async Task<string> ResetTokenAsync(HttpClient actor, Guid classId, Guid userId)
    {
        var response = await CreateResetAsync(actor, classId, userId);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var link = await response.Content.ReadFromJsonAsync<ApiFixture.Link>(Ct);
        Assert.StartsWith("/aterstall#", link!.Url);
        return ApiFixture.TokenFrom(link.Url);
    }

    // ---------- Återställa lösenord ----------

    [Fact]
    public async Task Teacher_resets_password_for_student()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var email = NewEmail();
        var (studentSession, student) = await api.CreateAccountAsync(classId, teacher, email, OldPassword);
        var token = await ResetTokenAsync(api.As(teacher), classId, student);
        var browser = api.Browser();

        var response = await CompleteAsync(browser, token);

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal(HttpStatusCode.OK, (await browser.GetAsync("/api/auth/me", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await studentSession.GetAsync("/api/auth/me", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await LoginAsync(api.Browser(), email, OldPassword)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await LoginAsync(api.Browser(), email, NewPassword)).StatusCode);
    }

    [Fact]
    public async Task Reset_link_is_valid_for_24_hours_and_records_creator()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var (_, student) = await api.CreateAccountAsync(classId, teacher, NewEmail());

        var response = await CreateResetAsync(api.As(teacher), classId, student);

        var link = await response.Content.ReadFromJsonAsync<ApiFixture.Link>(Ct);
        Assert.InRange(link!.ExpiresAt, DateTime.UtcNow.AddHours(24).AddMinutes(-1), DateTime.UtcNow.AddHours(24).AddMinutes(1));
        await using var connection = await api.OpenAsync();
        Assert.Equal(teacher, await connection.ExecuteScalarAsync<Guid>(
            "SELECT CreatedBy FROM password_resets WHERE TokenHash = @hash",
            new { hash = SHA256.HashData(Encoding.UTF8.GetBytes(ApiFixture.TokenFrom(link.Url))) }));
    }

    [Fact]
    public async Task Reset_clears_lockout()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var email = NewEmail();
        var (_, student) = await api.CreateAccountAsync(classId, teacher, email, OldPassword);
        for (var i = 0; i < 5; i++) await LoginAsync(api.Browser(), email, "fel-lösenord-som-är-långt");
        var token = await ResetTokenAsync(api.As(teacher), classId, student);

        await CompleteAsync(api.Browser(), token);

        Assert.Equal(HttpStatusCode.NoContent, (await LoginAsync(api.Browser(), email, NewPassword)).StatusCode);
    }

    [Fact]
    public async Task Teacher_cannot_reset_password_for_another_teacher()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var (_, secondTeacher) = await api.CreateAccountAsync(classId, teacher, NewEmail(), role: "Teacher");

        var response = await CreateResetAsync(api.As(teacher), classId, secondTeacher);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_cannot_reset_password_for_administrator()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var admin = await api.CreateUserAsync(isAdministrator: true);
        await api.As(teacher).PutAsJsonAsync($"/api/classes/{classId}/members/{admin}", new { role = "Student" }, Ct);

        var response = await CreateResetAsync(api.As(teacher), classId, admin);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Administrator_can_reset_password_for_teacher()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var admin = await api.CreateUserAsync(isAdministrator: true);

        var response = await CreateResetAsync(api.As(admin), classId, teacher);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
    }

    [Theory]
    [InlineData("Student")]
    [InlineData("StudentRepresentative")]
    public async Task Member_who_is_not_teacher_cannot_reset_passwords(string role)
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var (_, actor) = await api.CreateAccountAsync(classId, teacher, NewEmail(), role: role);
        var (_, target) = await api.CreateAccountAsync(classId, teacher, NewEmail());

        var response = await CreateResetAsync(api.As(actor), classId, target);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Reset_for_user_who_is_not_member_gives_404()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var outsider = await api.CreateUserAsync();

        var response = await CreateResetAsync(api.As(teacher), classId, outsider);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Teacher_of_another_class_cannot_reset_passwords()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var (_, student) = await api.CreateAccountAsync(classId, teacher, NewEmail());
        var (_, otherTeacher) = await api.CreateClassWithTeacherAsync();

        var response = await CreateResetAsync(api.As(otherTeacher), classId, student);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Reset_link_can_only_be_used_once_and_expires()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var (_, student) = await api.CreateAccountAsync(classId, teacher, NewEmail());
        var used = await ResetTokenAsync(api.As(teacher), classId, student);
        await CompleteAsync(api.Browser(), used);
        var expired = await ResetTokenAsync(api.As(teacher), classId, student);
        await using (var connection = await api.OpenAsync())
            await connection.ExecuteAsync(
                "UPDATE password_resets SET ExpiresAt = UTC_TIMESTAMP(6) - INTERVAL 1 SECOND WHERE TokenHash = @hash",
                new { hash = SHA256.HashData(Encoding.UTF8.GetBytes(expired)) });

        Assert.Equal(HttpStatusCode.NotFound, (await CompleteAsync(api.Browser(), used, "ännu-ett-nytt-lösenord")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await CompleteAsync(api.Browser(), expired, "ännu-ett-nytt-lösenord")).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await CompleteAsync(api.Browser(), "okänd-token")).StatusCode);
    }

    [Fact]
    public async Task Too_short_new_password_gives_400_and_keeps_link()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var (_, student) = await api.CreateAccountAsync(classId, teacher, NewEmail());
        var token = await ResetTokenAsync(api.As(teacher), classId, student);

        var tooShort = await CompleteAsync(api.Browser(), token, "elva-tecken");
        var ok = await CompleteAsync(api.Browser(), token);

        Assert.Equal(HttpStatusCode.BadRequest, tooShort.StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, ok.StatusCode);
    }

    // ---------- Medlemslista ----------

    [Fact]
    public async Task Teacher_sees_members_sorted_by_name()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        foreach (var name in new[] { "Örjan", "Zara", "Åsa" })
        {
            var token = await api.InviteAsync(classId, teacher, "Student");
            await api.Browser().PostAsJsonAsync("/api/invitations/accept",
                new { token, displayName = name, email = NewEmail(), password = OldPassword }, Ct);
        }

        var members = await api.As(teacher).GetFromJsonAsync<Member[]>($"/api/classes/{classId}/members", Ct);

        Assert.Equal(["Lärare", "Zara", "Åsa", "Örjan"], members!.Select(m => m.DisplayName));
        Assert.Equal("Teacher", members![0].Role);
    }

    [Fact]
    public async Task Administrator_sees_members()
    {
        var (classId, _) = await api.CreateClassWithTeacherAsync();
        var admin = await api.CreateUserAsync(isAdministrator: true);

        var response = await api.As(admin).GetAsync($"/api/classes/{classId}/members", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
    }

    [Fact]
    public async Task Student_cannot_see_member_list()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var (_, student) = await api.CreateAccountAsync(classId, teacher, NewEmail());

        var response = await api.As(student).GetAsync($"/api/classes/{classId}/members", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Non_member_cannot_see_member_list()
    {
        var (classId, _) = await api.CreateClassWithTeacherAsync();
        var (_, otherTeacher) = await api.CreateClassWithTeacherAsync();

        var response = await api.As(otherTeacher).GetAsync($"/api/classes/{classId}/members", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    sealed record Member(Guid UserId, string DisplayName, string Role);
}
