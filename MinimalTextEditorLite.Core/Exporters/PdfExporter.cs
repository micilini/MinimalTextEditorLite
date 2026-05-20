using MinimalTextEditorLite.Core.Rendering;

namespace MinimalTextEditorLite.Core.Exporters;

public sealed class PdfExporter : IExporter
{
    private readonly HtmlDocumentBuilder htmlBuilder;
    private readonly IPdfRenderer pdfRenderer;

    public PdfExporter(HtmlDocumentBuilder htmlBuilder, IPdfRenderer pdfRenderer)
    {
        this.htmlBuilder = htmlBuilder;
        this.pdfRenderer = pdfRenderer;
    }

    public string Id => "pdf";

    public string DisplayName => "PDF";

    public string DefaultFileName => "Note.pdf";

    public string FileDialogFilter => "PDF Files (*.pdf)|*.pdf";

    public async Task<byte[]> ExportAsync(ExportContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var html = htmlBuilder.Build(context.Document, new HtmlBuildOptions
        {
            Variant = HtmlVariant.Print,
            DocumentTitle = "Note Export"
        });

        var options = new PdfRenderOptions
        {
            PageWidthInches = 8.27,
            PageHeightInches = 11.69,
            MarginInches = 0.75,
            PrintBackgrounds = true,
            PrintHeaderFooter = false,
            Timeout = TimeSpan.FromSeconds(30)
        };

        return await pdfRenderer.RenderHtmlToPdfAsync(html, options);
    }
}
