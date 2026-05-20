namespace MinimalTextEditorLite.Core.Services;

public interface IBackupService
{
    Task CreateBackupAsync(string json, string dateTimeText);
    Task<BackupFolderStatistics> GetStatisticsAsync();
    Task RemoveAllAsync();
}
