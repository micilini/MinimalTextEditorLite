using MinimalTextEditorLite.Core.Models;

namespace MinimalTextEditorLite.Core.Repositories;

public interface IRecentFilesRepository
{
    Task<IReadOnlyList<RecentFileModel>> GetAllAsync();
    Task RegisterAsync(string filePath);
    Task RemoveAsync(string filePath);
    Task ClearAsync();
}
