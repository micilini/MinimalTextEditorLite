using Microsoft.Win32;
using System.IO;

namespace MinimalTextEditorLite.App.Helpers;

public static class ShellExtensionInstaller
{
    private const string JsonProgId = "MinimalTextEditor.Json";
    private const string MarkdownProgId = "MinimalTextEditor.Markdown";
    private const string LegacyJsonProgId = "MinimalTextEditorLite.Json";
    private const string LegacyMarkdownProgId = "MinimalTextEditorLite.Markdown";

    public static bool IsInstalled()
    {
        var exePath = Environment.ProcessPath;

        if (string.IsNullOrWhiteSpace(exePath))
            return false;

        return IsCommandInstalled(JsonProgId, exePath) &&
               IsCommandInstalled(MarkdownProgId, exePath);
    }

    public static void Install()
    {
        var exePath = Environment.ProcessPath;

        if (string.IsNullOrWhiteSpace(exePath) || !File.Exists(exePath))
            throw new InvalidOperationException("Application executable path was not found.");

        RemoveExtension(".json", LegacyJsonProgId);
        RemoveExtension(".md", LegacyMarkdownProgId);
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{LegacyJsonProgId}", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{LegacyMarkdownProgId}", throwOnMissingSubKey: false);

        RegisterExtension(".json", JsonProgId, "Minimal Text Editor Lite JSON", exePath);
        RegisterExtension(".md", MarkdownProgId, "Minimal Text Editor Lite Markdown", exePath);
    }

    public static void Uninstall()
    {
        RemoveExtension(".json", JsonProgId);
        RemoveExtension(".md", MarkdownProgId);
        RemoveExtension(".json", LegacyJsonProgId);
        RemoveExtension(".md", LegacyMarkdownProgId);

        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{JsonProgId}", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{MarkdownProgId}", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{LegacyJsonProgId}", throwOnMissingSubKey: false);
        Registry.CurrentUser.DeleteSubKeyTree($@"Software\Classes\{LegacyMarkdownProgId}", throwOnMissingSubKey: false);
    }

    private static void RegisterExtension(string extension, string progId, string description, string exePath)
    {
        using var openWithKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{extension}\OpenWithProgids");
        openWithKey?.SetValue(progId, string.Empty, RegistryValueKind.String);

        using var progIdKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}");
        progIdKey?.SetValue(string.Empty, description, RegistryValueKind.String);

        using var commandKey = Registry.CurrentUser.CreateSubKey($@"Software\Classes\{progId}\shell\open\command");
        commandKey?.SetValue(string.Empty, $"\"{exePath}\" \"%1\"", RegistryValueKind.String);
    }

    private static void RemoveExtension(string extension, string progId)
    {
        using var openWithKey = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{extension}\OpenWithProgids", writable: true);
        openWithKey?.DeleteValue(progId, throwOnMissingValue: false);
    }

    private static bool IsCommandInstalled(string progId, string exePath)
    {
        using var commandKey = Registry.CurrentUser.OpenSubKey($@"Software\Classes\{progId}\shell\open\command");
        var value = commandKey?.GetValue(string.Empty)?.ToString();

        return value?.Contains(exePath, StringComparison.OrdinalIgnoreCase) == true;
    }
}
