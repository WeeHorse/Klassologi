using System.Data;
using Dapper;
using Klassologi.Api.Auth;
using Klassologi.Api.Data;
using Klassologi.Api.Features.Classes;

namespace Klassologi.Api.Features.Posts;

/// <summary>
/// Steg 4, anslagstavlan: UC-08 Publicera, UC-09 Läsa, UC-10 Redigera och ta bort samt fästning.
/// Se Specifications/UseCases.md. Texten lagras som Markdown och renderas i klienten.
/// </summary>
public static class PostEndpoints
{
    public const int MaxTitleLength = 150;
    public const int MaxBodyLength = 10_000;

    public static void MapPostEndpoints(this WebApplication app)
    {
        var posts = app.MapGroup("/api/classes/{classId:guid}/posts").RequireAuthorization();
        posts.MapGet("/", GetBoard);
        posts.MapPost("/", Publish);
        posts.MapPut("/{postId:guid}", Edit);
        posts.MapDelete("/{postId:guid}", Delete);
        posts.MapPut("/{postId:guid}/pin", Pin);
    }

    public sealed record PostRequest(string? Title, string? Body);
    public sealed record PinRequest(bool Pinned);
    public sealed record AuthorResponse(Guid Id, string DisplayName, ClassRole? Role);
    public sealed record DeletedResponse(DateTime At, string ByDisplayName, DateTime PurgeAt);
    public sealed record PostResponse(
        Guid Id, string Title, string Body, AuthorResponse Author,
        DateTime CreatedAt, DateTime? EditedAt, DateTime? PinnedAt, DeletedResponse? Deleted);

    sealed class PostRow
    {
        public Guid Id { get; set; }
        public string Title { get; set; } = "";
        public string Body { get; set; } = "";
        public Guid AuthorId { get; set; }
        public string AuthorName { get; set; } = "";
        public string? AuthorRole { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? EditedAt { get; set; }
        public DateTime? PinnedAt { get; set; }
        public DateTime? DeletedAt { get; set; }
        public string? DeletedByName { get; set; }
    }

    const string SelectPosts =
        """
        SELECT p.Id, p.Title, p.Body, p.AuthorId, a.DisplayName AS AuthorName, m.Role AS AuthorRole,
               p.CreatedAt, p.EditedAt, p.PinnedAt, p.DeletedAt, d.DisplayName AS DeletedByName
        FROM posts p
        JOIN users a ON a.Id = p.AuthorId
        LEFT JOIN class_memberships m ON m.ClassId = p.ClassId AND m.UserId = p.AuthorId
        LEFT JOIN users d ON d.Id = p.DeletedBy
        """;

    static DateTime Utc(DateTime value) => DateTime.SpecifyKind(value, DateTimeKind.Utc);
    static DateTime? Utc(DateTime? value) => value is { } v ? Utc(v) : null;

    static PostResponse ToResponse(PostRow r) => new(
        r.Id, r.Title, r.Body,
        new AuthorResponse(r.AuthorId, r.AuthorName, r.AuthorRole is null ? null : Enum.Parse<ClassRole>(r.AuthorRole)),
        Utc(r.CreatedAt), Utc(r.EditedAt), Utc(r.PinnedAt),
        r.DeletedAt is { } deletedAt
            ? new DeletedResponse(Utc(deletedAt), r.DeletedByName ?? "", Utc(deletedAt).Add(PostPurge.Retention))
            : null);

    static async Task<PostResponse> LoadAsync(IDbConnection connection, Guid postId) =>
        ToResponse(await connection.QuerySingleAsync<PostRow>($"{SelectPosts} WHERE p.Id = @postId", new { postId }));

    /// <summary>Ett inlägg som finns i klassen och inte är borttaget.</summary>
    static Task<Guid?> FindAuthorAsync(IDbConnection connection, Guid classId, Guid postId) =>
        connection.QuerySingleOrDefaultAsync<Guid?>(
            "SELECT AuthorId FROM posts WHERE Id = @postId AND ClassId = @classId AND DeletedAt IS NULL",
            new { postId, classId });

    static IResult PostNotFound() => Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Inlägget finns inte.");

    static (string Title, string Body, IResult? Error) Validate(PostRequest request)
    {
        var title = request.Title?.Trim() ?? "";
        var body = request.Body?.Trim() ?? "";
        var errors = new Dictionary<string, string[]>();
        if (title.Length is 0 or > MaxTitleLength) errors["title"] = [$"Rubriken måste vara 1–{MaxTitleLength} tecken."];
        if (body.Length is 0 or > MaxBodyLength) errors["body"] = [$"Texten måste vara 1–{MaxBodyLength:# ###} tecken."];
        return (title, body, errors.Count > 0 ? Results.ValidationProblem(errors) : null);
    }

    // UC-09
    static async Task<IResult> GetBoard(Guid classId, HttpContext http, Db db, CancellationToken ct)
    {
        if (CurrentUser.Id(http.User) is not { } userId) return Results.Unauthorized();
        await using var connection = await db.OpenAsync(ct);
        var access = await ClassAccess.CheckAsync(connection, classId, userId);
        if (access.Deny(access.CanRead) is { } denied) return denied;

        // Borttagna inlägg visas bara för administratörer.
        var rows = await connection.QueryAsync<PostRow>(
            $"""
            {SelectPosts}
            WHERE p.ClassId = @classId AND (p.DeletedAt IS NULL OR @includeDeleted)
            ORDER BY p.PinnedAt IS NULL, p.PinnedAt DESC, p.CreatedAt DESC, p.Id DESC
            """,
            new { classId, includeDeleted = access.IsAdministrator });
        return Results.Ok(rows.Select(ToResponse));
    }

    // UC-08
    static async Task<IResult> Publish(Guid classId, PostRequest request, HttpContext http, Db db, CancellationToken ct)
    {
        if (CurrentUser.Id(http.User) is not { } userId) return Results.Unauthorized();
        await using var connection = await db.OpenAsync(ct);
        var access = await ClassAccess.CheckAsync(connection, classId, userId);
        if (access.Deny(access.Role is ClassRole.Teacher or ClassRole.StudentRepresentative) is { } denied) return denied;

        var (title, body, error) = Validate(request);
        if (error is not null) return error;

        var postId = Guid.CreateVersion7();
        await connection.ExecuteAsync(
            "INSERT INTO posts (Id, ClassId, AuthorId, Title, Body) VALUES (@postId, @classId, @userId, @title, @body)",
            new { postId, classId, userId, title, body });
        return Results.Json(await LoadAsync(connection, postId), statusCode: StatusCodes.Status201Created);
    }

    // UC-10
    static async Task<IResult> Edit(Guid classId, Guid postId, PostRequest request, HttpContext http, Db db, CancellationToken ct)
    {
        if (CurrentUser.Id(http.User) is not { } userId) return Results.Unauthorized();
        await using var connection = await db.OpenAsync(ct);
        var access = await ClassAccess.CheckAsync(connection, classId, userId);
        if (access.Deny(access.CanRead) is { } denied) return denied;

        if (await FindAuthorAsync(connection, classId, postId) is not { } authorId) return PostNotFound();
        if (authorId != userId) return Results.StatusCode(StatusCodes.Status403Forbidden);

        var (title, body, error) = Validate(request);
        if (error is not null) return error;

        await connection.ExecuteAsync(
            "UPDATE posts SET Title = @title, Body = @body, EditedAt = UTC_TIMESTAMP(6) WHERE Id = @postId",
            new { postId, title, body });
        return Results.Ok(await LoadAsync(connection, postId));
    }

    // UC-10
    static async Task<IResult> Delete(Guid classId, Guid postId, HttpContext http, Db db, CancellationToken ct)
    {
        if (CurrentUser.Id(http.User) is not { } userId) return Results.Unauthorized();
        await using var connection = await db.OpenAsync(ct);
        var access = await ClassAccess.CheckAsync(connection, classId, userId);
        if (access.Deny(access.CanRead) is { } denied) return denied;

        if (await FindAuthorAsync(connection, classId, postId) is not { } authorId) return PostNotFound();
        if (authorId != userId) return Results.StatusCode(StatusCodes.Status403Forbidden);

        // Inlägget döljs men sparas, och gallras efter 90 dagar (PostPurge).
        await connection.ExecuteAsync(
            "UPDATE posts SET DeletedAt = UTC_TIMESTAMP(6), DeletedBy = @userId WHERE Id = @postId",
            new { postId, userId });
        return Results.NoContent();
    }

    static async Task<IResult> Pin(Guid classId, Guid postId, PinRequest request, HttpContext http, Db db, CancellationToken ct)
    {
        if (CurrentUser.Id(http.User) is not { } userId) return Results.Unauthorized();
        await using var connection = await db.OpenAsync(ct);
        var access = await ClassAccess.CheckAsync(connection, classId, userId);
        if (access.Deny(access.Role == ClassRole.Teacher) is { } denied) return denied;

        if (await FindAuthorAsync(connection, classId, postId) is null) return PostNotFound();

        await connection.ExecuteAsync(
            request.Pinned
                ? "UPDATE posts SET PinnedAt = UTC_TIMESTAMP(6), PinnedBy = @userId WHERE Id = @postId"
                : "UPDATE posts SET PinnedAt = NULL, PinnedBy = NULL WHERE Id = @postId",
            new { postId, userId });
        return Results.NoContent();
    }
}
