using MinimalTextEditorLite.Core.Database;
using System.Security.Cryptography;

namespace MinimalTextEditorLite.Core.Security;

public interface IDpapiKeyStore
{
    string GetOrCreateHexKey();
}

public sealed class DpapiKeyStore : IDpapiKeyStore
{
    private static string ProtectedKeyPath => Path.Combine(AppPaths.LocalAppDataFolder, "db.key");

    public string GetOrCreateHexKey()
    {
        if (!OperatingSystem.IsWindows())
            throw new PlatformNotSupportedException("DPAPI database keys are only supported on Windows.");

        Directory.CreateDirectory(AppPaths.LocalAppDataFolder);

        if (!File.Exists(ProtectedKeyPath))
        {
            var rawKey = RandomNumberGenerator.GetBytes(32);
            var protectedKey = ProtectedData.Protect(rawKey, optionalEntropy: null, DataProtectionScope.CurrentUser);
            File.WriteAllBytes(ProtectedKeyPath, protectedKey);
            return Convert.ToHexString(rawKey);
        }

        var encryptedKey = File.ReadAllBytes(ProtectedKeyPath);
        var unprotectedKey = ProtectedData.Unprotect(encryptedKey, optionalEntropy: null, DataProtectionScope.CurrentUser);

        if (unprotectedKey.Length != 32)
            throw new InvalidOperationException("The protected database key is invalid.");

        return Convert.ToHexString(unprotectedKey);
    }
}
