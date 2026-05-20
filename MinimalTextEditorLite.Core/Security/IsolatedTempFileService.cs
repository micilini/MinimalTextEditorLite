using MinimalTextEditorLite.Core.Database;
using System.Security.AccessControl;
using System.Security.Principal;

namespace MinimalTextEditorLite.Core.Security;

public interface IIsolatedTempFileService
{
    string CreateTempJsonPath();

    string CreateTempFilePath(string extension);

    string CreateTempOutputPath(string extension);
}

public sealed class IsolatedTempFileService : IIsolatedTempFileService
{
    private static string TempFolder => Path.Combine(AppPaths.LocalAppDataFolder, "Temp");

    public string CreateTempJsonPath()
    {
        return CreateTempFilePath(".json");
    }

    public string CreateTempFilePath(string extension)
    {
        EnsureTempFolder();

        var normalizedExtension = NormalizeExtension(extension);
        return Path.Combine(TempFolder, $"{Guid.NewGuid():N}{normalizedExtension}");
    }

    public string CreateTempOutputPath(string extension)
    {
        // Backward-compatible alias kept for existing callers.
        // New code should prefer CreateTempFilePath.
        return CreateTempFilePath(extension);
    }

    private static string NormalizeExtension(string extension)
    {
        if (string.IsNullOrWhiteSpace(extension))
            throw new ArgumentException("A temporary file extension is required.", nameof(extension));

        var trimmedExtension = extension.Trim();
        return trimmedExtension.StartsWith('.') ? trimmedExtension : $".{trimmedExtension}";
    }

    private static void EnsureTempFolder()
    {
        Directory.CreateDirectory(TempFolder);

        if (!OperatingSystem.IsWindows())
            return;

        var currentUser = WindowsIdentity.GetCurrent().User;
        if (currentUser == null)
            return;

        var directoryInfo = new DirectoryInfo(TempFolder);
        var security = new DirectorySecurity();
        security.SetAccessRuleProtection(isProtected: true, preserveInheritance: false);
        security.AddAccessRule(new FileSystemAccessRule(
            currentUser,
            FileSystemRights.FullControl,
            InheritanceFlags.ContainerInherit | InheritanceFlags.ObjectInherit,
            PropagationFlags.None,
            AccessControlType.Allow));

        directoryInfo.SetAccessControl(security);
    }
}
