namespace MinimalTextEditorLite.Core.Rendering;

public sealed class PdfRenderOptions
{
    /// <summary>
    /// Page width in inches. Default is A4 portrait width: 8.27 inches.
    /// </summary>
    public double PageWidthInches { get; init; } = 8.27;

    /// <summary>
    /// Page height in inches. Default is A4 portrait height: 11.69 inches.
    /// </summary>
    public double PageHeightInches { get; init; } = 11.69;

    /// <summary>
    /// Uniform page margin in inches.
    /// </summary>
    public double MarginInches { get; init; } = 0.75;

    /// <summary>
    /// Keeps CSS backgrounds and colors during PDF printing.
    /// This is important for quote, warning and code blocks.
    /// </summary>
    public bool PrintBackgrounds { get; init; } = true;

    /// <summary>
    /// Controls whether the browser prints default headers/footers such as URL and date.
    /// The Lite editor should keep this disabled by default.
    /// </summary>
    public bool PrintHeaderFooter { get; init; }

    /// <summary>
    /// Total timeout for HTML rendering and PDF generation.
    /// </summary>
    public TimeSpan Timeout { get; init; } = TimeSpan.FromSeconds(30);
}
