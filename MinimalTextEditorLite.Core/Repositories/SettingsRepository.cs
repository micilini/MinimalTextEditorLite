using MinimalTextEditorLite.Core.Database;
using MinimalTextEditorLite.Core.Models;

namespace MinimalTextEditorLite.Core.Repositories;

public sealed class SettingsRepository(ISqliteConnectionFactory connectionFactory) : ISettingsRepository
{
    public Task<SettingsModel?> GetCurrentAsync()
    {
        var conn = connectionFactory.GetConnection();
        conn.CreateTable<SettingsModel>();
        return Task.FromResult(conn.Table<SettingsModel>().FirstOrDefault(settings => settings.Id == 1));
    }

    public Task<bool> UpdateAsync(SettingsModel settings)
    {
        var conn = connectionFactory.GetConnection();
        conn.CreateTable<SettingsModel>();
        return Task.FromResult(conn.Update(settings) > 0);
    }
}
