using Dapper;
using Klassologi.Api.Data;

namespace Klassologi.Api.Features.Status;

/// <summary>Visar att API:t når databasen och hur många migreringar som har körts.</summary>
public static class StatusEndpoints
{
    public static void MapStatusEndpoints(this WebApplication app) =>
        app.MapGet("/api/status", GetStatus);

    static async Task<StatusResponse> GetStatus(Db db, CancellationToken cancellationToken)
    {
        await using var connection = await db.OpenAsync(cancellationToken);
        var migrations = await connection.ExecuteScalarAsync<int>(
            new CommandDefinition("SELECT COUNT(*) FROM schemaversions", cancellationToken: cancellationToken));
        return new StatusResponse("ok", migrations);
    }
}

public sealed record StatusResponse(string Status, int Migrations);
