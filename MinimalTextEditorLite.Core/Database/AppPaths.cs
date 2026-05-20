namespace MinimalTextEditorLite.Core.Database;

public static class AppPaths
{
    public const string AppName = "MinimalTextEditorLite";

    public static string LocalAppDataFolder =>
        Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData), AppName);

    public static string BackupsFolder =>
        Path.Combine(LocalAppDataFolder, "Backups");
}
