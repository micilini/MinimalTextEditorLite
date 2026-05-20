namespace MinimalTextEditorLite.Core.Importers;

public sealed class JsonImporter : IImporter
{
    public string Extension => ".json";

    public Task<ImportedNoteContent> ImportAsync(string content, string filePath)
    {
        return Task.FromResult(new ImportedNoteContent
        {
            EditorJsJson = content
        });
    }
}
