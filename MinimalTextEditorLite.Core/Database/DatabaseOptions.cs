namespace MinimalTextEditorLite.Core.Database;

public sealed class DatabaseOptions
{
    public string DatabaseFileName { get; init; } = "mte-lite.dll";

    public string EncryptionKey { get; set; } = string.Empty;

    public string ProtectedKeyFilePath => Path.Combine(AppPaths.LocalAppDataFolder, "db.key");

    public string LegacyKeyFilePath => Path.Combine(AppPaths.LocalAppDataFolder, "dt-app.mte");

    public string KeyFilePath => LegacyKeyFilePath;

    public string DatabasePath => Path.Combine(AppPaths.LocalAppDataFolder, DatabaseFileName);
}
