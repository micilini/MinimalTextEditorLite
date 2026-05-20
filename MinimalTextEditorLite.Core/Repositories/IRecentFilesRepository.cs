namespace MinimalTextEditorLite.Core.Repositories;

public interface IRecentFilesRepository
{
    Task RegisterAsync(string filePath);
}
