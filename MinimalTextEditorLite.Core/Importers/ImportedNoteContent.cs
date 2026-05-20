using MinimalTextEditorLite.Core.Models;

namespace MinimalTextEditorLite.Core.Importers;

public sealed class ImportedNoteContent
{
    public required string EditorJsJson { get; init; }
    public NoteMetadata? Metadata { get; init; }
}
