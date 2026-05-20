namespace MinimalTextEditorLite.Core.Services;

public interface IImportService
{
    Task<ImportResult> ImportAsync(string filePath);
}
