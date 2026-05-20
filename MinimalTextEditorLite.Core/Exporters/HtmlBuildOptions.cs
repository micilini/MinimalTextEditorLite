namespace MinimalTextEditorLite.Core.Exporters;

public enum HtmlVariant
{
    /// <summary>HTML stand-alone para o usuário abrir no browser ou alimentar HtmlToOpenXml.</summary>
    Standard,

    /// <summary>HTML otimizado para impressão via WebView2.</summary>
    Print
}

public sealed class HtmlBuildOptions
{
    public HtmlVariant Variant { get; init; } = HtmlVariant.Standard;

    public string DocumentTitle { get; init; } = "Note Export";
}
