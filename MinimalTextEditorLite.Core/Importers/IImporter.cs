namespace MinimalTextEditorLite.Core.Importers;

public interface IImporter
{
    string Extension { get; }
    Task<ImportedNoteContent> ImportAsync(string content, string filePath);
}
