using MinimalTextEditorLite.Core.Database;
using SQLite;

namespace MinimalTextEditorLite.Core.Security;

public sealed class DatabaseKeyMigrationService(DatabaseOptions databaseOptions, IDpapiKeyStore keyStore)
{
    public void EnsureMigrated()
    {
        Directory.CreateDirectory(AppPaths.LocalAppDataFolder);

        if (!File.Exists(databaseOptions.DatabasePath))
        {
            databaseOptions.EncryptionKey = keyStore.GetOrCreateHexKey();
            return;
        }

        if (File.Exists(databaseOptions.ProtectedKeyFilePath))
        {
            databaseOptions.EncryptionKey = keyStore.GetOrCreateHexKey();
            VerifyDatabaseCanOpen(databaseOptions.EncryptionKey);
            return;
        }

        var backupPath = CreateLegacyBackup();

        try
        {
            foreach (var legacyKey in GetLegacyKeys())
            {
                if (TryRekeyDatabase(legacyKey, out var newKey))
                {
                    databaseOptions.EncryptionKey = newKey;
                    return;
                }
            }
        }
        catch
        {
            RestoreBackup(backupPath);
            DeleteProtectedKeyCreatedDuringFailedMigration();
            throw;
        }

        RestoreBackup(backupPath);
        DeleteProtectedKeyCreatedDuringFailedMigration();
        throw new InvalidOperationException("Could not migrate the legacy database key.");
    }

    private string CreateLegacyBackup()
    {
        var timestamp = DateTime.Now.ToString("yyyyMMdd-HHmmss");
        var backupPath = Path.Combine(
            AppPaths.LocalAppDataFolder,
            $"{Path.GetFileNameWithoutExtension(databaseOptions.DatabaseFileName)}.legacy-backup-{timestamp}{Path.GetExtension(databaseOptions.DatabaseFileName)}");

        File.Copy(databaseOptions.DatabasePath, backupPath, overwrite: false);
        return backupPath;
    }

    private IEnumerable<string> GetLegacyKeys()
    {
        if (File.Exists(databaseOptions.LegacyKeyFilePath))
            yield return File.ReadAllText(databaseOptions.LegacyKeyFilePath);

        yield return string.Empty;
    }

    private bool TryRekeyDatabase(string oldKey, out string newKey)
    {
        newKey = string.Empty;

        try
        {
            using var connection = OpenConnection(oldKey);
            connection.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master;");

            newKey = keyStore.GetOrCreateHexKey();
            connection.Execute($"PRAGMA rekey = '{EscapePragmaValue(newKey)}';");
            connection.Close();

            VerifyDatabaseCanOpen(newKey);
            return true;
        }
        catch
        {
            return false;
        }
    }

    private void VerifyDatabaseCanOpen(string key)
    {
        using var connection = OpenConnection(key);
        connection.ExecuteScalar<int>("SELECT COUNT(*) FROM sqlite_master;");
    }

    private SQLiteConnection OpenConnection(string key)
    {
        var connectionString = new SQLiteConnectionString(
            databaseOptions.DatabasePath,
            storeDateTimeAsTicks: true,
            key: key);

        return new SQLiteConnection(connectionString);
    }

    private void RestoreBackup(string backupPath)
    {
        if (File.Exists(backupPath))
            File.Copy(backupPath, databaseOptions.DatabasePath, overwrite: true);
    }

    private void DeleteProtectedKeyCreatedDuringFailedMigration()
    {
        if (File.Exists(databaseOptions.ProtectedKeyFilePath))
            File.Delete(databaseOptions.ProtectedKeyFilePath);
    }

    private static string EscapePragmaValue(string value)
    {
        return value.Replace("'", "''");
    }
}
