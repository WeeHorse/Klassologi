using MySqlConnector;

namespace Klassologi.Api.Data;

/// <summary>Öppnar anslutningar mot databasen. All SQL skrivs explicit med Dapper där den används.</summary>
public sealed class Db(string connectionString)
{
    public string ConnectionString { get; } = connectionString;

    public async Task<MySqlConnection> OpenAsync(CancellationToken cancellationToken = default)
    {
        var connection = new MySqlConnection(ConnectionString);
        await connection.OpenAsync(cancellationToken);
        return connection;
    }
}
