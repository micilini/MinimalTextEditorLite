using MinimalTextEditorLite.Core.Database;
using System.Security.AccessControl;
using System.Security.Principal;

namespace MinimalTextEditorLite.Core.Security;

public interface IIsolatedTempFileService
{
    string CreateTempJsonPath();

    string CreateTempOutputPath(string extension);
}

public sealed class IsolatedTempFileService : IIsolatedTempFileService
{
    private static string TempFolder => Path.Combine(AppPaths.LocalAppDataFolder, "Temp");

    public string CreateTempJsonPath()
    {
        EnsureTempFolder();
        return Path.Combine(TempFolder, $"{Guid.NewGuid():N}.json");
    }

    public string CreateTempOutputPath(string extension)
    {
        EnsureTempFolder();

        var normalizedExtension = extension.StartsWith('.') ? extension : $".{extension}";
        return Path.Combine(TempFolder, $"{Guid.NewGuid():N}{normalizedExtension}");
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
