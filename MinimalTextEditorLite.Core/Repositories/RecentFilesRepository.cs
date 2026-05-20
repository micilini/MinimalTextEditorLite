using MinimalTextEditorLite.Core.Database;
using MinimalTextEditorLite.Core.Models;

namespace MinimalTextEditorLite.Core.Repositories;

public sealed class RecentFilesRepository(ISqliteConnectionFactory connectionFactory) : IRecentFilesRepository
{
    private const int MaxRecentFiles = 5;

    public Task<IReadOnlyList<RecentFileModel>> GetAllAsync()
    {
        var connection = connectionFactory.GetConnection();

        IReadOnlyList<RecentFileModel> result = connection
            .Table<RecentFileModel>()
            .OrderByDescending(file => file.OpenedAt)
            .Take(MaxRecentFiles)
            .ToList();

        return Task.FromResult(result);
    }

    public Task RegisterAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Task.CompletedTask;

        var fullPath = Path.GetFullPath(filePath);
        var connection = connectionFactory.GetConnection();

        var existing = connection
            .Table<RecentFileModel>()
            .FirstOrDefault(file => file.FilePath == fullPath);

        if (existing == null)
        {
            connection.Insert(new RecentFileModel
            {
                FilePath = fullPath,
                FileName = Path.GetFileName(fullPath),
                OpenedAt = DateTime.UtcNow
            });
        }
        else
        {
            existing.FileName = Path.GetFileName(fullPath);
            existing.OpenedAt = DateTime.UtcNow;
            connection.Update(existing);
        }

        Trim(connection);

        return Task.CompletedTask;
    }

    public Task RemoveAsync(string filePath)
    {
        if (string.IsNullOrWhiteSpace(filePath))
            return Task.CompletedTask;

        var fullPath = Path.GetFullPath(filePath);
        var connection = connectionFactory.GetConnection();

        var existing = connection
            .Table<RecentFileModel>()
            .FirstOrDefault(file => file.FilePath == fullPath);

        if (existing != null)
            connection.Delete(existing);

        return Task.CompletedTask;
    }

    public Task ClearAsync()
    {
        var connection = connectionFactory.GetConnection();
        connection.DeleteAll<RecentFileModel>();

        return Task.CompletedTask;
    }

    private static void Trim(SQLite.SQLiteConnection connection)
    {
        var extraFiles = connection
            .Table<RecentFileModel>()
            .OrderByDescending(file => file.OpenedAt)
            .Skip(MaxRecentFiles)
            .ToList();

        foreach (var file in extraFiles)
            connection.Delete(file);
    }
}
