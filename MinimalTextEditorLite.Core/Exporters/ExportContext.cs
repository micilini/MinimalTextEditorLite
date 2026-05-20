using MinimalTextEditorLite.Core.Models;
using MinimalTextEditorLite.Exporters.Contracts.EditorJs;

namespace MinimalTextEditorLite.Core.Exporters;

public sealed class ExportContext
{
    public required EditorJsDocument Document { get; init; }
    public required NoteModel Note { get; init; }
    public required SettingsModel Settings { get; init; }
}
