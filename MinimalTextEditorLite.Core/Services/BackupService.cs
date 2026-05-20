using MinimalTextEditorLite.Core.Database;

namespace MinimalTextEditorLite.Core.Services;

public sealed class BackupService : IBackupService
{
    public async Task CreateBackupAsync(string json, string dateTimeText)
    {
        Directory.CreateDirectory(AppPaths.BackupsFolder);

        string backupFileName = $"backup_{dateTimeText.Replace('/', '-').Replace(':', '-')}.json";
        string backupFilePath = Path.Combine(AppPaths.BackupsFolder, backupFileName);

        await File.WriteAllTextAsync(backupFilePath, json);
    }

    public Task<BackupFolderStatistics> GetStatisticsAsync()
    {
        if (!Directory.Exists(AppPaths.BackupsFolder))
            return Task.FromResult(new BackupFolderStatistics("0", "0 MB"));

        var files = Directory.GetFiles(AppPaths.BackupsFolder);
        long totalSizeBytes = files.Sum(file => new FileInfo(file).Length);

        string totalSize = totalSizeBytes >= 1_073_741_824
            ? $"{totalSizeBytes / 1_073_741_824.0:F2} GB"
            : $"{totalSizeBytes / 1_048_576.0:F2} MB";

        return Task.FromResult(new BackupFolderStatistics($"{files.Length}", totalSize));
    }

    public Task RemoveAllAsync()
    {
        if (!Directory.Exists(AppPaths.BackupsFolder))
            return Task.CompletedTask;

        foreach (var file in Directory.GetFiles(AppPaths.BackupsFolder))
            File.Delete(file);

        return Task.CompletedTask;
    }
}
