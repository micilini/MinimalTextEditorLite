namespace MinimalTextEditorLite.Core.Exporters;

public sealed class MarkdownAssetExportResult
{
    public required string MarkdownFilePath { get; init; }
    public required string AssetsDirectoryPath { get; init; }
    public int AssetsWritten { get; init; }
}
