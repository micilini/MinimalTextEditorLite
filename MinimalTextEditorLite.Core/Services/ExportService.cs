using MinimalTextEditorLite.Core.Exporters;
using MinimalTextEditorLite.Core.Repositories;
using MinimalTextEditorLite.Exporters.Contracts.EditorJs;
using System.Text.Json;

namespace MinimalTextEditorLite.Core.Services;

public sealed class ExportService : IExportService
{
    private readonly IReadOnlyDictionary<string, IExporter> exporters;
    private readonly INoteRepository noteRepository;

    public ExportService(IEnumerable<IExporter> exporters, INoteRepository noteRepository)
    {
        this.exporters = exporters.ToDictionary(exporter => exporter.Id, StringComparer.OrdinalIgnoreCase);
        this.noteRepository = noteRepository;
    }

    public IReadOnlyList<ExporterDescriptor> GetExporters()
    {
        return exporters.Values
            .Select(exporter => new ExporterDescriptor(exporter.Id, exporter.DisplayName, exporter.DefaultFileName, exporter.FileDialogFilter))
            .ToList();
    }

    public ExporterDescriptor GetExporter(string exporterId)
    {
        if (!exporters.TryGetValue(exporterId, out var exporter))
            throw new InvalidOperationException($"Exporter '{exporterId}' was not found.");

        return new ExporterDescriptor(exporter.Id, exporter.DisplayName, exporter.DefaultFileName, exporter.FileDialogFilter);
    }

    public async Task<byte[]> ExportAsync(string exporterId)
    {
        if (!exporters.TryGetValue(exporterId, out var exporter))
            throw new InvalidOperationException($"Exporter '{exporterId}' was not found.");

        var note = await noteRepository.GetCurrentAsync();
        if (note == null)
            throw new InvalidOperationException("Current note was not found.");

        EditorJsDocument? document;
        try
        {
            document = JsonSerializer.Deserialize<EditorJsDocument>(note.NoteJson, EditorJsJson.Options);
        }
        catch (JsonException ex)
        {
            throw new InvalidOperationException("Current note JSON is invalid.", ex);
        }

        if (document == null)
            throw new InvalidOperationException("Current note JSON is invalid.");

        return await exporter.ExportAsync(document);
    }
}
