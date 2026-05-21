using MinimalTextEditorLite.Core.Models;

namespace MinimalTextEditorLite.Core.Exporters;

public enum HtmlVariant
{
    /// <summary>HTML stand-alone para o usuario abrir no browser ou alimentar HtmlToOpenXml.</summary>
    Standard,

    /// <summary>HTML otimizado para impressao via WebView2.</summary>
    Print
}

public sealed class HtmlBuildOptions
{
    public HtmlVariant Variant { get; init; } = HtmlVariant.Standard;

    public string DocumentTitle { get; init; } = "Note Export";

    public NoteMetadata? Metadata { get; init; }

    public bool IncludeMetadataSummary { get; init; }
}
