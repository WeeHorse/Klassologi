using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;

namespace Klassologi.Api.Tests;

/// <summary>
/// Härledda från Specifications/UseCases.md: UC-01, UC-03 och "Läsa klasser", inklusive de gemensamma reglerna
/// för 401/403/404 (Q11) och regeln om minst en lärare.
/// </summary>
public sealed class ClassesTests(ApiFixture api)
{
    static CancellationToken Ct => TestContext.Current.CancellationToken;

    // ---------- Hjälpmetoder ----------

    Task<(Guid ClassId, Guid TeacherId)> CreateClassAsync(string name = "9A") => api.CreateClassWithTeacherAsync(name);

    static Task<HttpResponseMessage> SetRoleAsync(HttpClient client, Guid classId, Guid userId, string role) =>
        client.PutAsJsonAsync($"/api/classes/{classId}/members/{userId}", new { role }, Ct);

    async Task<string?> RoleOfAsync(Guid classId, Guid userId)
    {
        var response = await api.As(userId).GetAsync($"/api/classes/{classId}", Ct);
        if (response.StatusCode == HttpStatusCode.Forbidden) return null;
        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<ClassBody>(Ct))!.MyRole;
    }

    // ---------- UC-01 Skapa klass ----------

    [Fact]
    public async Task Administrator_creates_class_without_members_and_gets_teacher_invitation()
    {
        var admin = await api.CreateUserAsync(isAdministrator: true);

        var response = await api.As(admin).PostAsJsonAsync("/api/classes", new { name = "  9A  " }, Ct);

        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        var created = await response.Content.ReadFromJsonAsync<ApiFixture.CreatedClass>(Ct);
        Assert.Equal("9A", created!.Name);
        Assert.Equal($"/api/classes/{created.Id}", response.Headers.Location?.ToString());
        Assert.StartsWith("/aktivera#", created.TeacherInvitation.Url);
        await using var connection = await api.OpenAsync();
        Assert.Equal(0, await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM class_memberships WHERE ClassId = @id", new { id = created.Id }));
    }

    [Fact]
    public async Task Teacher_invitation_from_class_creation_makes_user_teacher()
    {
        var admin = await api.CreateUserAsync(isAdministrator: true);
        var created = await (await api.As(admin).PostAsJsonAsync("/api/classes", new { name = "9A" }, Ct))
            .Content.ReadFromJsonAsync<ApiFixture.CreatedClass>(Ct);
        var teacher = await api.CreateUserAsync();

        var accept = await api.As(teacher).PostAsJsonAsync(
            "/api/invitations/accept", new { token = ApiFixture.TokenFrom(created!.TeacherInvitation.Url) }, Ct);

        Assert.Equal(HttpStatusCode.OK, accept.StatusCode);
        Assert.Equal("Teacher", await RoleOfAsync(created.Id, teacher));
    }

    [Fact]
    public async Task Created_class_records_who_created_it_and_when()
    {
        var admin = await api.CreateUserAsync(isAdministrator: true);
        var before = DateTime.UtcNow.AddSeconds(-5);

        var response = await api.As(admin).PostAsJsonAsync("/api/classes", new { name = "9A" }, Ct);
        var created = await response.Content.ReadFromJsonAsync<ClassBody>(Ct);

        await using var connection = await api.OpenAsync();
        var row = await connection.QuerySingleAsync<(Guid CreatedBy, DateTime CreatedAt)>(
            "SELECT CreatedBy, CreatedAt FROM classes WHERE Id = @id", new { id = created!.Id });
        Assert.Equal(admin, row.CreatedBy);
        Assert.InRange(row.CreatedAt, before, DateTime.UtcNow.AddSeconds(5));
    }

    [Fact]
    public async Task Non_administrator_cannot_create_class()
    {
        var (_, teacher) = await CreateClassAsync();

        var response = await api.As(teacher).PostAsJsonAsync("/api/classes", new { name = "9B" }, Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_cannot_create_class()
    {
        var response = await api.Anonymous().PostAsJsonAsync("/api/classes", new { name = "9B" }, Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_gets_401_even_with_invalid_body()
    {
        var (classId, _) = await CreateClassAsync();
        var user = await api.CreateUserAsync();
        var anonymous = api.Anonymous();

        var brokenJson = await anonymous.PostAsync("/api/classes",
            new StringContent("{", System.Text.Encoding.UTF8, "application/json"), Ct);
        var unknownRole = await SetRoleAsync(anonymous, classId, user, "Rektor");

        Assert.Equal(HttpStatusCode.Unauthorized, brokenJson.StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, unknownRole.StatusCode);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task Class_name_must_not_be_empty(string name)
    {
        var admin = await api.CreateUserAsync(isAdministrator: true);

        var response = await api.As(admin).PostAsJsonAsync("/api/classes", new { name }, Ct);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.NotEmpty(await ValidationErrorsAsync(response, "name"));
    }

    [Theory]
    [InlineData(100, HttpStatusCode.Created)]
    [InlineData(101, HttpStatusCode.BadRequest)]
    public async Task Class_name_may_be_at_most_100_characters(int length, HttpStatusCode expected)
    {
        var admin = await api.CreateUserAsync(isAdministrator: true);

        var response = await api.As(admin).PostAsJsonAsync("/api/classes", new { name = new string('Ö', length) }, Ct);

        Assert.Equal(expected, response.StatusCode);
    }

    [Fact]
    public async Task Class_names_need_not_be_unique()
    {
        var (first, _) = await CreateClassAsync("Samma namn");
        var (second, _) = await CreateClassAsync("Samma namn");

        Assert.NotEqual(first, second);
    }

    // ---------- Läsa klasser och klassavgränsning (Q11) ----------

    [Fact]
    public async Task Member_can_read_class()
    {
        var (classId, teacher) = await CreateClassAsync("Åk 9 – Ängby");

        var response = await api.As(teacher).GetAsync($"/api/classes/{classId}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var body = await response.Content.ReadFromJsonAsync<ClassBody>(Ct);
        Assert.Equal("Åk 9 – Ängby", body!.Name);
    }

    [Fact]
    public async Task Non_member_gets_403_for_existing_class()
    {
        var (classId, _) = await CreateClassAsync();
        var (_, teacherOfOtherClass) = await CreateClassAsync();

        var response = await api.As(teacherOfOtherClass).GetAsync($"/api/classes/{classId}", Ct);

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
    }

    [Fact]
    public async Task Unknown_class_gives_404()
    {
        var user = await api.CreateUserAsync();

        var response = await api.As(user).GetAsync($"/api/classes/{Guid.CreateVersion7()}", Ct);

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_cannot_read_class()
    {
        var (classId, _) = await CreateClassAsync();

        var response = await api.Anonymous().GetAsync($"/api/classes/{classId}", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task Administrator_who_is_not_member_can_read_class_without_role()
    {
        var (classId, _) = await CreateClassAsync();
        var admin = await api.CreateUserAsync(isAdministrator: true);

        var response = await api.As(admin).GetAsync($"/api/classes/{classId}", Ct);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Null((await response.Content.ReadFromJsonAsync<ClassBody>(Ct))!.MyRole);
    }

    [Fact]
    public async Task Anonymous_cannot_list_classes()
    {
        var response = await api.Anonymous().GetAsync("/api/classes", Ct);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Fact]
    public async Task My_classes_lists_only_own_classes_in_swedish_order()
    {
        var admin = await api.CreateUserAsync(isAdministrator: true);
        var teacher = await api.CreateUserAsync();
        foreach (var name in new[] { "Öst", "Zeta", "Ängby", "Åby" })
        {
            var created = await (await api.As(admin).PostAsJsonAsync("/api/classes", new { name }, Ct))
                .Content.ReadFromJsonAsync<ApiFixture.CreatedClass>(Ct);
            await api.As(teacher).PostAsJsonAsync(
                "/api/invitations/accept", new { token = ApiFixture.TokenFrom(created!.TeacherInvitation.Url) }, Ct);
        }
        await CreateClassAsync("Annan lärares klass");

        var classes = await api.As(teacher).GetFromJsonAsync<ClassBody[]>("/api/classes", Ct);

        Assert.Equal(["Zeta", "Åby", "Ängby", "Öst"], classes!.Select(c => c.Name));
        Assert.All(classes!, c => Assert.Equal("Teacher", c.MyRole));
    }

    // ---------- UC-03 Tilldela och ändra roll ----------

    [Fact]
    public async Task Teacher_adds_user_as_student()
    {
        var (classId, teacher) = await CreateClassAsync();
        var student = await api.CreateUserAsync();

        var response = await SetRoleAsync(api.As(teacher), classId, student, "Student");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("Student", await RoleOfAsync(classId, student));
    }

    [Fact]
    public async Task Teacher_changes_role_of_existing_member()
    {
        var (classId, teacher) = await CreateClassAsync();
        var student = await api.CreateUserAsync();
        await SetRoleAsync(api.As(teacher), classId, student, "Student");

        var response = await SetRoleAsync(api.As(teacher), classId, student, "StudentRepresentative");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("StudentRepresentative", await RoleOfAsync(classId, student));
    }

    [Theory]
    [InlineData("Student")]
    [InlineData("StudentRepresentative")]
    public async Task Member_who_is_not_teacher_cannot_set_roles(string actorRole)
    {
        var (classId, teacher) = await CreateClassAsync();
        var actor = await api.CreateUserAsync();
        var target = await api.CreateUserAsync();
        await SetRoleAsync(api.As(teacher), classId, actor, actorRole);

        var response = await SetRoleAsync(api.As(actor), classId, target, "Student");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(await RoleOfAsync(classId, target));
    }

    [Fact]
    public async Task Teacher_of_another_class_cannot_set_roles()
    {
        var (classId, _) = await CreateClassAsync();
        var (_, teacherOfOtherClass) = await CreateClassAsync();

        var response = await SetRoleAsync(api.As(teacherOfOtherClass), classId, teacherOfOtherClass, "Teacher");

        Assert.Equal(HttpStatusCode.Forbidden, response.StatusCode);
        Assert.Null(await RoleOfAsync(classId, teacherOfOtherClass));
    }

    [Fact]
    public async Task Administrator_who_is_not_member_can_set_roles()
    {
        var (classId, _) = await CreateClassAsync();
        var admin = await api.CreateUserAsync(isAdministrator: true);
        var student = await api.CreateUserAsync();

        var response = await SetRoleAsync(api.As(admin), classId, student, "Student");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
    }

    [Fact]
    public async Task Anonymous_cannot_set_roles()
    {
        var (classId, _) = await CreateClassAsync();
        var user = await api.CreateUserAsync();

        var response = await SetRoleAsync(api.Anonymous(), classId, user, "Student");

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
    }

    [Theory]
    [InlineData("Admin")]
    [InlineData("Rektor")]
    [InlineData("")]
    [InlineData("0")]
    public async Task Invalid_role_gives_400(string role)
    {
        var (classId, teacher) = await CreateClassAsync();
        var user = await api.CreateUserAsync();

        var response = await SetRoleAsync(api.As(teacher), classId, user, role);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
    }

    [Fact]
    public async Task Role_is_case_insensitive_and_stored_in_canonical_form()
    {
        var (classId, teacher) = await CreateClassAsync();
        var user = await api.CreateUserAsync();

        var response = await SetRoleAsync(api.As(teacher), classId, user, "studentrepresentative");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("StudentRepresentative", await RoleOfAsync(classId, user));
    }

    [Fact]
    public async Task Unknown_user_gives_404()
    {
        var (classId, teacher) = await CreateClassAsync();

        var response = await SetRoleAsync(api.As(teacher), classId, Guid.CreateVersion7(), "Student");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await ProblemTitleAsync(response)));
    }

    [Fact]
    public async Task Setting_role_in_unknown_class_gives_404()
    {
        var admin = await api.CreateUserAsync(isAdministrator: true);
        var user = await api.CreateUserAsync();

        var response = await SetRoleAsync(api.As(admin), Guid.CreateVersion7(), user, "Student");

        Assert.Equal(HttpStatusCode.NotFound, response.StatusCode);
    }

    [Fact]
    public async Task Last_teacher_cannot_be_demoted()
    {
        var (classId, teacher) = await CreateClassAsync();

        var response = await SetRoleAsync(api.As(teacher), classId, teacher, "Student");

        Assert.Equal(HttpStatusCode.Conflict, response.StatusCode);
        Assert.False(string.IsNullOrWhiteSpace(await ProblemTitleAsync(response)));
        Assert.Equal("Teacher", await RoleOfAsync(classId, teacher));
    }

    [Fact]
    public async Task Teacher_can_be_demoted_when_another_teacher_remains()
    {
        var (classId, teacher) = await CreateClassAsync();
        var secondTeacher = await api.CreateUserAsync();
        await SetRoleAsync(api.As(teacher), classId, secondTeacher, "Teacher");

        var response = await SetRoleAsync(api.As(teacher), classId, teacher, "Student");

        Assert.Equal(HttpStatusCode.NoContent, response.StatusCode);
        Assert.Equal("Student", await RoleOfAsync(classId, teacher));
    }

    [Fact]
    public async Task Concurrent_demotions_never_leave_class_without_teacher()
    {
        var (classId, firstTeacher) = await CreateClassAsync();
        var teachers = new List<Guid> { firstTeacher };
        for (var i = 0; i < 9; i++)
        {
            var teacher = await api.CreateUserAsync();
            await SetRoleAsync(api.As(firstTeacher), classId, teacher, "Teacher");
            teachers.Add(teacher);
        }

        // Alla tio lärare försöker samtidigt göra sig själva till elever. Varje anrop är tillåtet var för sig,
        // men om alla gick igenom skulle klassen bli utan lärare.
        var responses = await Task.WhenAll(teachers.Select(t => SetRoleAsync(api.As(t), classId, t, "Student")));

        await using var connection = await api.OpenAsync();
        var remainingTeachers = await connection.ExecuteScalarAsync<int>(
            "SELECT COUNT(*) FROM class_memberships WHERE ClassId = @classId AND Role = 'Teacher'", new { classId });
        Assert.Equal(1, remainingTeachers);
        Assert.Equal(9, responses.Count(r => r.StatusCode == HttpStatusCode.NoContent));
        Assert.Equal(1, responses.Count(r => r.StatusCode == HttpStatusCode.Conflict));
    }

    static async Task<string[]> ValidationErrorsAsync(HttpResponseMessage response, string field)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return problem.RootElement.GetProperty("errors").GetProperty(field).EnumerateArray()
            .Select(e => e.GetString()!).ToArray();
    }

    static async Task<string?> ProblemTitleAsync(HttpResponseMessage response)
    {
        using var problem = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return problem.RootElement.TryGetProperty("title", out var title) ? title.GetString() : null;
    }

    sealed record ClassBody(Guid Id, string Name, string? MyRole);
}
