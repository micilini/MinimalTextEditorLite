using MinimalTextEditorLite.Core.Models;

namespace MinimalTextEditorLite.Core.Repositories;

public interface INoteRepository
{
    Task<NoteModel?> GetCurrentAsync();
    Task<bool> UpdateAsync(NoteModel note);
    Task<bool> UpdateJsonAsync(string json);
    Task<bool> ClearAsync();
}
