using SQLite;

namespace MinimalTextEditorLite.Core.Database;

public interface ISqliteConnectionFactory
{
    SQLiteConnection GetConnection();
}
