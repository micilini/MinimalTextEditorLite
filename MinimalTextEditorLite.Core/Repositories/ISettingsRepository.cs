using MinimalTextEditorLite.Core.Models;

namespace MinimalTextEditorLite.Core.Repositories;

public interface ISettingsRepository
{
    Task<SettingsModel?> GetCurrentAsync();
    Task<bool> UpdateAsync(SettingsModel settings);
}
