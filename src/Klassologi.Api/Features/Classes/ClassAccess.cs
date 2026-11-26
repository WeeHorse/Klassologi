using System.Data;
using Dapper;

namespace Klassologi.Api.Features.Classes;

/// <summary>
/// Den enda platsen som avgör en användares åtkomst till en klass. Varje klassbunden endpoint går via
/// <see cref="CheckAsync"/> och använder bara klass-id:t efter att åtkomsten har avgjorts här.
/// </summary>
public static class ClassAccess
{
    public sealed record Result(bool ClassExists, ClassRole? Role, bool IsAdministrator)
    {
        public bool CanRead => Role is not null || IsAdministrator;
        public bool CanManageMembers => Role == ClassRole.Teacher || IsAdministrator;

        /// <summary>404 om klassen inte finns, annars 403 (Q11). Null när åtkomst ges.</summary>
        public IResult? Deny(bool allowed) =>
            !ClassExists ? Results.NotFound() : allowed ? null : Results.StatusCode(StatusCodes.Status403Forbidden);
    }

    public static async Task<Result> CheckAsync(
        IDbConnection connection, Guid classId, Guid userId, IDbTransaction? transaction = null)
    {
        var row = await connection.QuerySingleAsync<(long ClassExists, string? Role, long IsAdministrator)>(
            """
            SELECT
                EXISTS (SELECT 1 FROM classes WHERE Id = @classId) AS ClassExists,
                (SELECT Role FROM class_memberships WHERE ClassId = @classId AND UserId = @userId) AS Role,
                COALESCE((SELECT IsAdministrator FROM users WHERE Id = @userId), FALSE) AS IsAdministrator
            """,
            new { classId, userId }, transaction);

        return new Result(
            row.ClassExists == 1,
            row.Role is null ? null : Enum.Parse<ClassRole>(row.Role),
            row.IsAdministrator == 1);
    }
}
