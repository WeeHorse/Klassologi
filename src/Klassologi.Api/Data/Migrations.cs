using DbUp;

namespace Klassologi.Api.Data;

/// <summary>
/// Kör versionerade SQL-skript i Data/Migrations i namnordning. Körda skript loggas i tabellen
/// schemaversions och körs aldrig igen. MySQL kan inte rulla tillbaka schemaändringar i en
/// transaktion, så skripten hålls små.
/// </summary>
public static class Migrations
{
    public static void Run(string connectionString, ILogger logger)
    {
        var result = DeployChanges.To
            .MySqlDatabase(connectionString)
            .WithScriptsEmbeddedInAssembly(typeof(Migrations).Assembly, name => name.Contains(".Data.Migrations."))
            .LogToNowhere()
            .Build()
            .PerformUpgrade();

        foreach (var script in result.Scripts)
            logger.LogInformation("Migrering körd: {Script}", script.Name);

        if (!result.Successful)
            throw new InvalidOperationException($"Migreringen misslyckades i {result.ErrorScript?.Name}.", result.Error);
    }
}
