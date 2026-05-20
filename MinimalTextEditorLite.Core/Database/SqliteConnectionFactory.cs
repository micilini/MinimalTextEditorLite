using SQLite;

namespace MinimalTextEditorLite.Core.Database;

public sealed class SqliteConnectionFactory(DatabaseOptions options) : ISqliteConnectionFactory
{
    private SQLiteConnection? _connection;

    public SQLiteConnection GetConnection()
    {
        if (_connection != null)
            return _connection;

        var connectionString = new SQLiteConnectionString(
            options.DatabasePath,
            storeDateTimeAsTicks: true,
            key: options.EncryptionKey
        );

        _connection = new SQLiteConnection(connectionString);
        return _connection;
    }
}
