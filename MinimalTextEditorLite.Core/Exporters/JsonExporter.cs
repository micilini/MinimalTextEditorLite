using MinimalTextEditorLite.Exporters.Contracts.EditorJs;
using System.Text;
using System.Text.Json;

namespace MinimalTextEditorLite.Core.Exporters;

public sealed class JsonExporter : IExporter
{
    public string Id => "json";
    public string DisplayName => "JSON";
    public string DefaultFileName => "Note.json";
    public string FileDialogFilter => "JSON Files (*.json)|*.json";

    public Task<byte[]> ExportAsync(EditorJsDocument document)
    {
        var json = JsonSerializer.Serialize(document, EditorJsJson.Options);
        return Task.FromResult(Encoding.UTF8.GetBytes(json));
    }
}
