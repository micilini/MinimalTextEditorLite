using MinimalTextEditorLite.Core.Exporters;

namespace MinimalTextEditorLite.Core.Services;

public interface IExportService
{
    IReadOnlyList<ExporterDescriptor> GetExporters();
    ExporterDescriptor GetExporter(string exporterId);
    Task<byte[]> ExportAsync(string exporterId);
    Task<MarkdownAssetExportResult> ExportMarkdownWithAssetsAsync(string outputDirectory);
}
