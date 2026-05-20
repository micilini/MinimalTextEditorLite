using MinimalTextEditorLite.Core.Database;
using MinimalTextEditorLite.Core.Models;

namespace MinimalTextEditorLite.Core.Repositories;

public sealed class NoteRepository(ISqliteConnectionFactory connectionFactory) : INoteRepository
{
    public Task<NoteModel?> GetCurrentAsync()
    {
        var conn = connectionFactory.GetConnection();
        conn.CreateTable<NoteModel>();
        return Task.FromResult(conn.Table<NoteModel>().FirstOrDefault(note => note.Id == 1));
    }

    public Task<bool> UpdateAsync(NoteModel note)
    {
        var conn = connectionFactory.GetConnection();
        conn.CreateTable<NoteModel>();
        return Task.FromResult(conn.Update(note) > 0);
    }

    public async Task<bool> UpdateJsonAsync(string json)
    {
        var note = await GetCurrentAsync();
        if (note == null)
            return false;

        note.NoteJson = json;
        note.UpdatedAt = DateTime.UtcNow;

        return await UpdateAsync(note);
    }

    public Task<bool> ClearAsync()
    {
        var conn = connectionFactory.GetConnection();
        conn.CreateTable<NoteModel>();
        var rowsAffected = conn.Execute("UPDATE Note SET NoteJson = ?, UpdatedAt = ? WHERE Id = ?", "{\"blocks\": []}", DateTime.UtcNow, 1);
        return Task.FromResult(rowsAffected > 0);
    }
}
