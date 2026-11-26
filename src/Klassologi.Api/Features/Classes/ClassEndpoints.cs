using Dapper;
using Klassologi.Api.Auth;
using Klassologi.Api.Data;

namespace Klassologi.Api.Features.Classes;

/// <summary>UC-01 Skapa klass, UC-03 Tilldela och ändra roll, samt läsning av klasser. Se Specifications/UseCases.md.</summary>
public static class ClassEndpoints
{
    public const int MaxNameLength = 100;

    public static void MapClassEndpoints(this WebApplication app)
    {
        var classes = app.MapGroup("/api/classes");
        classes.MapPost("/", CreateClass);
        classes.MapGet("/", GetMyClasses);
        classes.MapGet("/{classId:guid}", GetClass);
        classes.MapPut("/{classId:guid}/members/{userId:guid}", SetMemberRole);
    }

    public sealed record CreateClassRequest(string? Name, Guid FirstTeacherUserId);
    public sealed record SetMemberRoleRequest(ClassRole Role);
    public sealed record ClassResponse(Guid Id, string Name, ClassRole? MyRole);

    // UC-01
    static async Task<IResult> CreateClass(CreateClassRequest request, HttpContext http, Db db, CancellationToken ct)
    {
        if (CurrentUser.Id(http.User) is not { } actorId) return Results.Unauthorized();

        await using var connection = await db.OpenAsync(ct);
        var isAdministrator = await connection.ExecuteScalarAsync<bool>(
            "SELECT COALESCE((SELECT IsAdministrator FROM users WHERE Id = @actorId), FALSE)", new { actorId });
        if (!isAdministrator) return Results.StatusCode(StatusCodes.Status403Forbidden);

        var name = request.Name?.Trim() ?? "";
        if (name.Length is 0 or > MaxNameLength)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["name"] = [$"Klassens namn måste vara 1–{MaxNameLength} tecken."],
            });

        await using var transaction = await connection.BeginTransactionAsync(ct);
        var teacherExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM users WHERE Id = @id)", new { id = request.FirstTeacherUserId }, transaction);
        if (!teacherExists)
            return Results.ValidationProblem(new Dictionary<string, string[]>
            {
                ["firstTeacherUserId"] = ["Den utsedda läraren finns inte."],
            });

        var classId = Guid.CreateVersion7();
        await connection.ExecuteAsync(
            """
            INSERT INTO classes (Id, Name, CreatedBy) VALUES (@classId, @name, @actorId);
            INSERT INTO class_memberships (ClassId, UserId, Role) VALUES (@classId, @teacherId, 'Teacher');
            """,
            new { classId, name, actorId, teacherId = request.FirstTeacherUserId }, transaction);
        await transaction.CommitAsync(ct);

        return Results.Created($"/api/classes/{classId}", new ClassResponse(classId, name, ClassRole.Teacher));
    }

    static async Task<IResult> GetMyClasses(HttpContext http, Db db, CancellationToken ct)
    {
        if (CurrentUser.Id(http.User) is not { } userId) return Results.Unauthorized();

        await using var connection = await db.OpenAsync(ct);
        var rows = await connection.QueryAsync<(Guid Id, string Name, string Role)>(
            """
            SELECT c.Id, c.Name, m.Role
            FROM class_memberships m
            JOIN classes c ON c.Id = m.ClassId
            WHERE m.UserId = @userId
            ORDER BY c.Name, c.Id
            """,
            new { userId });

        return Results.Ok(rows.Select(r => new ClassResponse(r.Id, r.Name, Enum.Parse<ClassRole>(r.Role))));
    }

    static async Task<IResult> GetClass(Guid classId, HttpContext http, Db db, CancellationToken ct)
    {
        if (CurrentUser.Id(http.User) is not { } userId) return Results.Unauthorized();

        await using var connection = await db.OpenAsync(ct);
        var access = await ClassAccess.CheckAsync(connection, classId, userId);
        if (access.Deny(access.CanRead) is { } denied) return denied;

        var name = await connection.ExecuteScalarAsync<string>(
            "SELECT Name FROM classes WHERE Id = @classId", new { classId });
        return Results.Ok(new ClassResponse(classId, name!, access.Role));
    }

    // UC-03
    static async Task<IResult> SetMemberRole(
        Guid classId, Guid userId, SetMemberRoleRequest request, HttpContext http, Db db, CancellationToken ct)
    {
        if (CurrentUser.Id(http.User) is not { } actorId) return Results.Unauthorized();
        if (!Enum.IsDefined(request.Role)) return Results.BadRequest();

        await using var connection = await db.OpenAsync(ct);
        await using var transaction = await connection.BeginTransactionAsync(ct);

        // Lås klassen så att ändringar av medlemskap i samma klass sker en i taget (regeln om minst en lärare).
        await connection.ExecuteAsync(
            "SELECT Id FROM classes WHERE Id = @classId FOR UPDATE", new { classId }, transaction);

        var access = await ClassAccess.CheckAsync(connection, classId, actorId, transaction);
        if (access.Deny(access.CanManageMembers) is { } denied) return denied;

        var userExists = await connection.ExecuteScalarAsync<bool>(
            "SELECT EXISTS (SELECT 1 FROM users WHERE Id = @userId)", new { userId }, transaction);
        if (!userExists) return Results.Problem(statusCode: StatusCodes.Status404NotFound, title: "Användaren finns inte.");

        if (request.Role != ClassRole.Teacher)
        {
            var otherTeachers = await connection.ExecuteScalarAsync<int>(
                "SELECT COUNT(*) FROM class_memberships WHERE ClassId = @classId AND Role = 'Teacher' AND UserId <> @userId",
                new { classId, userId }, transaction);
            if (otherTeachers == 0)
                return Results.Problem(statusCode: StatusCodes.Status409Conflict, title: "Klassen måste ha minst en lärare.");
        }

        await connection.ExecuteAsync(
            """
            INSERT INTO class_memberships (ClassId, UserId, Role) VALUES (@classId, @userId, @role) AS new
            ON DUPLICATE KEY UPDATE Role = new.Role, UpdatedAt = UTC_TIMESTAMP(6)
            """,
            new { classId, userId, role = request.Role.ToString() }, transaction);
        await transaction.CommitAsync(ct);

        return Results.NoContent();
    }
}
