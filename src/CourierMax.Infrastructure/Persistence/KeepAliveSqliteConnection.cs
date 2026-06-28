using Microsoft.Data.Sqlite;

namespace CourierMax.Infrastructure.Persistence;

/// <summary>
/// Mantiene una conexión SQLite abierta para tests con ":memory:".
/// Esto garantiza que las tablas creadas con EnsureCreated persistan
/// entre scopes de EF (cada scope abre su propia conexión por defecto).
/// </summary>
public sealed class KeepAliveSqliteConnection : IDisposable
{
    public SqliteConnection Connection { get; }

    public KeepAliveSqliteConnection(string connString)
    {
        Connection = new SqliteConnection(connString);
        Connection.Open();
    }

    public void Dispose() => Connection.Dispose();
}
