using Dapper;
using Klassologi.Api.Data;

namespace Klassologi.Api.Features.Posts;

/// <summary>
/// Gallrar borttagna inlägg permanent 90 dagar efter borttagningen (steg 4). Körs vid start och sedan varje timme.
/// Flera instanser kan köra samtidigt utan problem, eftersom borttagningen är idempotent.
/// </summary>
public sealed class PostPurge(Db db, ILogger<PostPurge> logger) : BackgroundService
{
    public static readonly TimeSpan Retention = TimeSpan.FromDays(90);
    static readonly TimeSpan Interval = TimeSpan.FromHours(1);

    public static async Task<int> PurgeAsync(Db db, CancellationToken ct)
    {
        await using var connection = await db.OpenAsync(ct);
        return await connection.ExecuteAsync(new CommandDefinition(
            "DELETE FROM posts WHERE DeletedAt < UTC_TIMESTAMP(6) - INTERVAL 90 DAY", cancellationToken: ct));
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        using var timer = new PeriodicTimer(Interval);
        do
        {
            try
            {
                var purged = await PurgeAsync(db, stoppingToken);
                if (purged > 0) logger.LogInformation("Gallrade {Count} borttagna inlägg.", purged);
            }
            catch (Exception e) when (e is not OperationCanceledException)
            {
                logger.LogError(e, "Gallringen av borttagna inlägg misslyckades. Nytt försök om en timme.");
            }
        } while (await timer.WaitForNextTickAsync(stoppingToken));
    }
}
