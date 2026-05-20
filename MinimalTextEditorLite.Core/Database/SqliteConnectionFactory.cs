using SQLite;

namespace MinimalTextEditorLite.Core.Database;

public sealed class SqliteConnectionFactory(DatabaseOptions options) : ISqliteConnectionFactory
{
    private SQLiteConnection? _connection;

    public SQLiteConnection GetConnection()
    {
        if (_connection != null)
            return _connection;

        if (string.IsNullOrWhiteSpace(options.EncryptionKey))
            throw new InvalidOperationException("Database encryption key was not initialized.");

        var connectionString = new SQLiteConnectionString(
            options.DatabasePath,
            storeDateTimeAsTicks: true,
            key: options.EncryptionKey
        );

        _connection = new SQLiteConnection(connectionString);
        return _connection;
    }
}
