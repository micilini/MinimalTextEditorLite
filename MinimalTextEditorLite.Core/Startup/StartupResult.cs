using MinimalTextEditorLite.Core.Models;

namespace MinimalTextEditorLite.Core.Startup;

public sealed class StartupResult
{
    public required SettingsModel Settings { get; init; }

    public bool DatabaseAlreadyExisted { get; init; }
}
