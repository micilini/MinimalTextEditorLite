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
}
