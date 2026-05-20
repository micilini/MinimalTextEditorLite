using MinimalTextEditorLite.Core.Database;
using MinimalTextEditorLite.Core.Models;
using MinimalTextEditorLite.Core.Security;

namespace MinimalTextEditorLite.Core.Startup;

public sealed class StartupAppConfiguration(
    DatabaseOptions databaseOptions,
    ISqliteConnectionFactory connectionFactory,
    DatabaseKeyMigrationService databaseKeyMigrationService)
{
    public StartupResult CheckAndCreateDatabase()
    {
        Directory.CreateDirectory(AppPaths.LocalAppDataFolder);
        Directory.CreateDirectory(AppPaths.BackupsFolder);

        var databaseAlreadyExisted = File.Exists(databaseOptions.DatabasePath);
        databaseKeyMigrationService.EnsureMigrated();

        var connection = connectionFactory.GetConnection();

        connection.CreateTable<AppVersion>();
        connection.CreateTable<SettingsModel>();
        connection.CreateTable<NoteModel>();
        connection.CreateTable<RecentFileModel>();
        EnsureV2Columns(connection);

        if (!connection.Table<AppVersion>().Any())
            connection.Insert(new AppVersion());

        if (!connection.Table<SettingsModel>().Any())
            connection.Insert(new SettingsModel());

        if (!connection.Table<NoteModel>().Any())
            connection.Insert(new NoteModel());

        var settings = connection.Table<SettingsModel>().First();

        return new StartupResult
        {
            Settings = settings,
            DatabaseAlreadyExisted = databaseAlreadyExisted
        };
    }

    private static void EnsureV2Columns(SQLite.SQLiteConnection connection)
    {
        AddColumnIfMissing(connection, "Note", "Title", "Title TEXT");
        AddColumnIfMissing(connection, "Note", "Slug", "Slug TEXT");
        AddColumnIfMissing(connection, "Note", "Tags", "Tags TEXT");
        AddColumnIfMissing(connection, "Note", "PublishDate", "PublishDate TEXT");

        AddColumnIfMissing(connection, "Settings", "ExportFrontMatterYaml", "ExportFrontMatterYaml INTEGER NOT NULL DEFAULT 1");
        AddColumnIfMissing(connection, "Settings", "Theme", "Theme TEXT NOT NULL DEFAULT 'light'");
        AddColumnIfMissing(connection, "Settings", "AssociateFilesWithApp", "AssociateFilesWithApp INTEGER NOT NULL DEFAULT 0");
    }

    private static void AddColumnIfMissing(SQLite.SQLiteConnection connection, string tableName, string columnName, string columnDefinition)
    {
        var escapedTableName = tableName.Replace("'", "''");
        var escapedColumnName = columnName.Replace("'", "''");

        var exists = connection.ExecuteScalar<int>(
            $"SELECT COUNT(*) FROM pragma_table_info('{escapedTableName}') WHERE name = ?",
            escapedColumnName);

        if (exists == 0)
            connection.Execute($"ALTER TABLE {tableName} ADD COLUMN {columnDefinition}");
    }
}
