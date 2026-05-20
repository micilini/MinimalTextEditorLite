namespace MinimalTextEditorLite.Core.Exporters;

public interface IMarkdownAssetExporter
{
    Task<MarkdownAssetExportResult> ExportWithAssetsAsync(ExportContext context, string outputDirectory);
}
