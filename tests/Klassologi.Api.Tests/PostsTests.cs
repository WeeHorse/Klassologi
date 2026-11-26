using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using Dapper;
using Klassologi.Api.Data;
using Klassologi.Api.Features.Posts;

namespace Klassologi.Api.Tests;

/// <summary>Härledda från Specifications/UseCases.md, steg 4: UC-08, UC-09, UC-10, fästning och gallring.</summary>
public sealed class PostsTests(ApiFixture api)
{
    static CancellationToken Ct => TestContext.Current.CancellationToken;

    sealed record Author(Guid Id, string DisplayName, string? Role);
    sealed record Deleted(DateTime At, string ByDisplayName, DateTime PurgeAt);
    sealed record Post(Guid Id, string Title, string Body, Author Author, DateTime CreatedAt, DateTime? EditedAt, DateTime? PinnedAt, Deleted? Deleted);

    async Task<Guid> MemberAsync(Guid classId, Guid teacher, string role, string name = "Medlem")
    {
        var user = await api.CreateUserAsync(name);
        (await api.As(teacher).PutAsJsonAsync($"/api/classes/{classId}/members/{user}", new { role }, Ct)).EnsureSuccessStatusCode();
        return user;
    }

    static Task<HttpResponseMessage> PublishAsync(HttpClient client, Guid classId, string title = "Rubrik", string body = "Text") =>
        client.PostAsJsonAsync($"/api/classes/{classId}/posts", new { title, body }, Ct);

    static async Task<Post> PublishOkAsync(HttpClient client, Guid classId, string title = "Rubrik", string body = "Text")
    {
        var response = await PublishAsync(client, classId, title, body);
        Assert.Equal(HttpStatusCode.Created, response.StatusCode);
        return (await response.Content.ReadFromJsonAsync<Post>(Ct))!;
    }

    Task<Post[]?> BoardAsync(Guid userId, Guid classId) =>
        api.As(userId).GetFromJsonAsync<Post[]>($"/api/classes/{classId}/posts", Ct);

    static async Task<bool> HasFieldErrorAsync(HttpResponseMessage response, string field)
    {
        using var json = JsonDocument.Parse(await response.Content.ReadAsStringAsync(Ct));
        return json.RootElement.TryGetProperty("errors", out var errors) && errors.TryGetProperty(field, out _);
    }

    // ---------- UC-08 Publicera ----------

    [Fact]
    public async Task Teacher_publishes_post()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();

        var post = await PublishOkAsync(api.As(teacher), classId, "  Schema vecka 48  ", "**Viktigt:** ny sal.");

        Assert.Equal("Schema vecka 48", post.Title);
        Assert.Equal("**Viktigt:** ny sal.", post.Body);
        Assert.Equal(teacher, post.Author.Id);
        Assert.Equal("Teacher", post.Author.Role);
        Assert.Null(post.EditedAt);
        Assert.Null(post.PinnedAt);
        Assert.Null(post.Deleted);
    }

    [Fact]
    public async Task Student_representative_publishes_post()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var representative = await MemberAsync(classId, teacher, "StudentRepresentative");

        Assert.Equal(HttpStatusCode.Created, (await PublishAsync(api.As(representative), classId)).StatusCode);
    }

    [Fact]
    public async Task Student_cannot_publish()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var student = await MemberAsync(classId, teacher, "Student");

        Assert.Equal(HttpStatusCode.Forbidden, (await PublishAsync(api.As(student), classId)).StatusCode);
    }

    [Fact]
    public async Task Administrator_who_is_not_member_cannot_publish()
    {
        var (classId, _) = await api.CreateClassWithTeacherAsync();
        var admin = await api.CreateUserAsync(isAdministrator: true);

        Assert.Equal(HttpStatusCode.Forbidden, (await PublishAsync(api.As(admin), classId)).StatusCode);
    }

    [Fact]
    public async Task Teacher_of_another_class_cannot_publish()
    {
        var (classId, _) = await api.CreateClassWithTeacherAsync();
        var (_, otherTeacher) = await api.CreateClassWithTeacherAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await PublishAsync(api.As(otherTeacher), classId)).StatusCode);
    }

    [Fact]
    public async Task Publishing_requires_login_and_existing_class()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();

        Assert.Equal(HttpStatusCode.Unauthorized, (await PublishAsync(api.Anonymous(), classId)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PublishAsync(api.As(teacher), Guid.CreateVersion7())).StatusCode);
    }

    [Theory]
    [InlineData("", "Text", "title")]
    [InlineData("   ", "Text", "title")]
    [InlineData("Rubrik", "", "body")]
    [InlineData("Rubrik", "   ", "body")]
    public async Task Empty_title_or_body_gives_400(string title, string body, string field)
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();

        var response = await PublishAsync(api.As(teacher), classId, title, body);

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(await HasFieldErrorAsync(response, field));
    }

    [Theory]
    [InlineData(150, 10_000, HttpStatusCode.Created)]
    [InlineData(151, 10, HttpStatusCode.BadRequest)]
    [InlineData(10, 10_001, HttpStatusCode.BadRequest)]
    public async Task Title_at_most_150_and_body_at_most_10000_characters(int titleLength, int bodyLength, HttpStatusCode expected)
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();

        var response = await PublishAsync(api.As(teacher), classId, new string('Å', titleLength), new string('ö', bodyLength));

        Assert.Equal(expected, response.StatusCode);
    }

    // ---------- UC-09 Läsa ----------

    [Fact]
    public async Task Members_and_administrators_can_read_the_board()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var student = await MemberAsync(classId, teacher, "Student");
        var admin = await api.CreateUserAsync(isAdministrator: true);
        await PublishOkAsync(api.As(teacher), classId);

        Assert.Single((await BoardAsync(student, classId))!);
        Assert.Single((await BoardAsync(admin, classId))!);
    }

    [Fact]
    public async Task Non_member_gets_403_and_anonymous_401()
    {
        var (classId, _) = await api.CreateClassWithTeacherAsync();
        var (_, otherTeacher) = await api.CreateClassWithTeacherAsync();

        Assert.Equal(HttpStatusCode.Forbidden, (await api.As(otherTeacher).GetAsync($"/api/classes/{classId}/posts", Ct)).StatusCode);
        Assert.Equal(HttpStatusCode.Unauthorized, (await api.Anonymous().GetAsync($"/api/classes/{classId}/posts", Ct)).StatusCode);
    }

    [Fact]
    public async Task Board_only_shows_posts_of_its_own_class()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var (otherClass, otherTeacher) = await api.CreateClassWithTeacherAsync();
        await PublishOkAsync(api.As(otherTeacher), otherClass, "Annan klass");

        Assert.Empty((await BoardAsync(teacher, classId))!);
    }

    [Fact]
    public async Task Pinned_posts_first_latest_pinned_first_then_newest_first()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var a = await PublishOkAsync(api.As(teacher), classId, "A");
        var b = await PublishOkAsync(api.As(teacher), classId, "B");
        await PublishOkAsync(api.As(teacher), classId, "C");
        await PublishOkAsync(api.As(teacher), classId, "D");
        await PinAsync(teacher, classId, a.Id, true);
        await PinAsync(teacher, classId, b.Id, true);

        var board = await BoardAsync(teacher, classId);

        Assert.Equal(["B", "A", "D", "C"], board!.Select(p => p.Title));
        Assert.NotNull(board![0].PinnedAt);
        Assert.Null(board[2].PinnedAt);
    }

    [Fact]
    public async Task Author_role_is_the_current_role_in_the_class()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var representative = await MemberAsync(classId, teacher, "StudentRepresentative");
        await PublishOkAsync(api.As(representative), classId);
        await api.As(teacher).PutAsJsonAsync($"/api/classes/{classId}/members/{representative}", new { role = "Student" }, Ct);

        var board = await BoardAsync(teacher, classId);

        Assert.Equal("Student", board!.Single().Author.Role);
    }

    // ---------- UC-10 Redigera och ta bort ----------

    static Task<HttpResponseMessage> EditAsync(HttpClient client, Guid classId, Guid postId, string title = "Ny rubrik", string body = "Ny text") =>
        client.PutAsJsonAsync($"/api/classes/{classId}/posts/{postId}", new { title, body }, Ct);

    static Task<HttpResponseMessage> DeleteAsync(HttpClient client, Guid classId, Guid postId) =>
        client.DeleteAsync($"/api/classes/{classId}/posts/{postId}", Ct);

    [Fact]
    public async Task Author_edits_post_and_it_is_marked_edited()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var post = await PublishOkAsync(api.As(teacher), classId);

        var response = await EditAsync(api.As(teacher), classId, post.Id);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var edited = await response.Content.ReadFromJsonAsync<Post>(Ct);
        Assert.Equal("Ny rubrik", edited!.Title);
        Assert.Equal("Ny text", edited.Body);
        Assert.NotNull(edited.EditedAt);
    }

    [Fact]
    public async Task Edit_validates_like_publishing()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var post = await PublishOkAsync(api.As(teacher), classId);

        var response = await EditAsync(api.As(teacher), classId, post.Id, title: "");

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.True(await HasFieldErrorAsync(response, "title"));
    }

    [Fact]
    public async Task Others_cannot_edit_or_delete_even_teachers()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var representative = await MemberAsync(classId, teacher, "StudentRepresentative");
        var post = await PublishOkAsync(api.As(representative), classId);

        Assert.Equal(HttpStatusCode.Forbidden, (await EditAsync(api.As(teacher), classId, post.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.Forbidden, (await DeleteAsync(api.As(teacher), classId, post.Id)).StatusCode);
    }

    [Fact]
    public async Task Author_keeps_right_to_edit_and_delete_after_role_change()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var representative = await MemberAsync(classId, teacher, "StudentRepresentative");
        var post = await PublishOkAsync(api.As(representative), classId);
        await api.As(teacher).PutAsJsonAsync($"/api/classes/{classId}/members/{representative}", new { role = "Student" }, Ct);

        Assert.Equal(HttpStatusCode.OK, (await EditAsync(api.As(representative), classId, post.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NoContent, (await DeleteAsync(api.As(representative), classId, post.Id)).StatusCode);
    }

    [Fact]
    public async Task Deleted_post_is_hidden_for_members_but_shown_to_administrators()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var student = await MemberAsync(classId, teacher, "Student");
        var admin = await api.CreateUserAsync(isAdministrator: true);
        var post = await PublishOkAsync(api.As(teacher), classId);

        Assert.Equal(HttpStatusCode.NoContent, (await DeleteAsync(api.As(teacher), classId, post.Id)).StatusCode);

        Assert.Empty((await BoardAsync(student, classId))!);
        Assert.Empty((await BoardAsync(teacher, classId))!);
        var seenByAdmin = Assert.Single((await BoardAsync(admin, classId))!);
        Assert.NotNull(seenByAdmin.Deleted);
        Assert.Equal("Lärare", seenByAdmin.Deleted!.ByDisplayName);
        Assert.Equal(seenByAdmin.Deleted.At.AddDays(90), seenByAdmin.Deleted.PurgeAt, TimeSpan.FromSeconds(1));
    }

    [Fact]
    public async Task Deleted_post_cannot_be_edited_deleted_or_pinned()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var post = await PublishOkAsync(api.As(teacher), classId);
        await DeleteAsync(api.As(teacher), classId, post.Id);

        Assert.Equal(HttpStatusCode.NotFound, (await EditAsync(api.As(teacher), classId, post.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await DeleteAsync(api.As(teacher), classId, post.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await PinRequestAsync(teacher, classId, post.Id, true)).StatusCode);
    }

    [Fact]
    public async Task Post_from_another_class_gives_404_through_own_class()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var (otherClass, otherTeacher) = await api.CreateClassWithTeacherAsync();
        var otherPost = await PublishOkAsync(api.As(otherTeacher), otherClass);

        // Läraren har behörighet i sin egen klass, men inlägget hör till en annan klass.
        Assert.Equal(HttpStatusCode.NotFound, (await PinRequestAsync(teacher, classId, otherPost.Id, true)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await EditAsync(api.As(teacher), classId, otherPost.Id)).StatusCode);
        Assert.Equal(HttpStatusCode.NotFound, (await DeleteAsync(api.As(teacher), classId, otherPost.Id)).StatusCode);
        Assert.Null((await BoardAsync(otherTeacher, otherClass))!.Single().PinnedAt);
    }

    // ---------- Fästa ----------

    Task<HttpResponseMessage> PinRequestAsync(Guid userId, Guid classId, Guid postId, bool pinned) =>
        api.As(userId).PutAsJsonAsync($"/api/classes/{classId}/posts/{postId}/pin", new { pinned }, Ct);

    async Task PinAsync(Guid userId, Guid classId, Guid postId, bool pinned) =>
        Assert.Equal(HttpStatusCode.NoContent, (await PinRequestAsync(userId, classId, postId, pinned)).StatusCode);

    [Fact]
    public async Task Teacher_pins_and_unpins_any_post()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var representative = await MemberAsync(classId, teacher, "StudentRepresentative");
        var post = await PublishOkAsync(api.As(representative), classId);

        await PinAsync(teacher, classId, post.Id, true);
        Assert.NotNull((await BoardAsync(teacher, classId))!.Single().PinnedAt);

        await PinAsync(teacher, classId, post.Id, false);
        Assert.Null((await BoardAsync(teacher, classId))!.Single().PinnedAt);
    }

    [Theory]
    [InlineData("StudentRepresentative")]
    [InlineData("Student")]
    public async Task Only_teachers_can_pin(string role)
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var member = await MemberAsync(classId, teacher, role);
        var post = await PublishOkAsync(api.As(teacher), classId);

        Assert.Equal(HttpStatusCode.Forbidden, (await PinRequestAsync(member, classId, post.Id, true)).StatusCode);
    }

    [Fact]
    public async Task Administrator_who_is_not_member_cannot_pin()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var admin = await api.CreateUserAsync(isAdministrator: true);
        var post = await PublishOkAsync(api.As(teacher), classId);

        Assert.Equal(HttpStatusCode.Forbidden, (await PinRequestAsync(admin, classId, post.Id, true)).StatusCode);
    }

    // ---------- Gallring ----------

    [Fact]
    public async Task Purge_removes_posts_deleted_more_than_90_days_ago_only()
    {
        var (classId, teacher) = await api.CreateClassWithTeacherAsync();
        var old = await PublishOkAsync(api.As(teacher), classId, "Gammal");
        var recent = await PublishOkAsync(api.As(teacher), classId, "Nyligen");
        await PublishOkAsync(api.As(teacher), classId, "Kvar");
        await DeleteAsync(api.As(teacher), classId, old.Id);
        await DeleteAsync(api.As(teacher), classId, recent.Id);
        await using (var connection = await api.OpenAsync())
        {
            await connection.ExecuteAsync("UPDATE posts SET DeletedAt = UTC_TIMESTAMP(6) - INTERVAL 91 DAY WHERE Id = @id", new { id = old.Id });
            await connection.ExecuteAsync("UPDATE posts SET DeletedAt = UTC_TIMESTAMP(6) - INTERVAL 89 DAY WHERE Id = @id", new { id = recent.Id });
        }

        await PostPurge.PurgeAsync(new Db(api.ConnectionString), Ct);

        await using var check = await api.OpenAsync();
        var remaining = (await check.QueryAsync<string>("SELECT Title FROM posts WHERE ClassId = @classId ORDER BY Title", new { classId })).ToArray();
        Assert.Equal(["Kvar", "Nyligen"], remaining);
    }
}
