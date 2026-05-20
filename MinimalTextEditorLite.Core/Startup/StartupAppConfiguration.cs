using MinimalTextEditorLite.Core.Database;
using MinimalTextEditorLite.Core.Models;

namespace MinimalTextEditorLite.Core.Startup;

public sealed class StartupAppConfiguration(
    DatabaseOptions databaseOptions,
    ISqliteConnectionFactory connectionFactory)
{
    public StartupResult CheckAndCreateDatabase()
    {
        Directory.CreateDirectory(AppPaths.LocalAppDataFolder);
        Directory.CreateDirectory(AppPaths.BackupsFolder);

        if (!File.Exists(databaseOptions.KeyFilePath))
        {
            var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
            File.WriteAllText(databaseOptions.KeyFilePath, timestamp);
        }

        databaseOptions.EncryptionKey = File.ReadAllText(databaseOptions.KeyFilePath);

        var databaseAlreadyExisted = File.Exists(databaseOptions.DatabasePath);

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
