namespace MinimalTextEditorLite.Core.Database;

public sealed class DatabaseOptions
{
    public string DatabaseFileName { get; init; } = "mte-lite.dll";

    public string EncryptionKey { get; set; } = string.Empty;

    public string KeyFilePath => Path.Combine(AppPaths.LocalAppDataFolder, "dt-app.mte");

    public string DatabasePath => Path.Combine(AppPaths.LocalAppDataFolder, DatabaseFileName);
}
