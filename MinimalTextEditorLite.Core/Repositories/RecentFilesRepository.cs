namespace MinimalTextEditorLite.Core.Repositories;

public sealed class RecentFilesRepository : IRecentFilesRepository
{
    public Task RegisterAsync(string filePath)
    {
        // F8 will persist and expose recent files in the UI.
        return Task.CompletedTask;
    }
}
