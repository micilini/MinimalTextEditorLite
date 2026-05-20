using MinimalTextEditorLite.Core.Exporters;
using MinimalTextEditorLite.Core.Repositories;
using MinimalTextEditorLite.Exporters.Contracts.EditorJs;
using System.Text.Json;

namespace MinimalTextEditorLite.Core.Services;

public sealed class ExportService : IExportService
{
    private readonly IReadOnlyDictionary<string, IExporter> exporters;
    private readonly INoteRepository noteRepository;
    private readonly ISettingsRepository settingsRepository;

    public ExportService(IEnumerable<IExporter> exporters, INoteRepository noteRepository, ISettingsRepository settingsRepository)
    {
        this.exporters = exporters.ToDictionary(exporter => exporter.Id, StringComparer.OrdinalIgnoreCase);
        this.noteRepository = noteRepository;
        this.settingsRepository = settingsRepository;
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

        var context = await CreateContextAsync();
        return await exporter.ExportAsync(context);
    }

    public async Task<MarkdownAssetExportResult> ExportMarkdownWithAssetsAsync(string outputDirectory)
    {
        if (!exporters.TryGetValue("md", out var exporter))
            throw new InvalidOperationException("Markdown exporter was not found.");

        if (exporter is not IMarkdownAssetExporter markdownAssetExporter)
            throw new InvalidOperationException("Markdown exporter does not support asset folder export.");

        var context = await CreateContextAsync();
        return await markdownAssetExporter.ExportWithAssetsAsync(context, outputDirectory);
    }

    private async Task<ExportContext> CreateContextAsync()
    {
        var note = await noteRepository.GetCurrentAsync();
        if (note == null)
            throw new InvalidOperationException("Current note was not found.");

        var settings = await settingsRepository.GetCurrentAsync();
        if (settings == null)
            throw new InvalidOperationException("Current settings were not found.");

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

        var context = new ExportContext
        {
            Document = document,
            Note = note,
            Settings = settings
        };

        return context;
    }
}
