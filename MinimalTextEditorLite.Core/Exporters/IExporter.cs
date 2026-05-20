using MinimalTextEditorLite.Exporters.Contracts.EditorJs;

namespace MinimalTextEditorLite.Core.Exporters;

public interface IExporter
{
    string Id { get; }
    string DisplayName { get; }
    string DefaultFileName { get; }
    string FileDialogFilter { get; }

    Task<byte[]> ExportAsync(EditorJsDocument document);
}
