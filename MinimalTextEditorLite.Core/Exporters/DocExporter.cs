using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Wordprocessing;
using HtmlToOpenXml;
using MinimalTextEditorLite.Exporters.Contracts.EditorJs;

namespace MinimalTextEditorLite.Core.Exporters;

public sealed class DocExporter : IExporter
{
    private readonly HtmlDocumentBuilder htmlBuilder;

    public DocExporter(HtmlDocumentBuilder htmlBuilder)
    {
        this.htmlBuilder = htmlBuilder;
    }

    public string Id => "doc";
    public string DisplayName => "DOCX";
    public string DefaultFileName => "Note.docx";
    public string FileDialogFilter => "Word Documents (*.docx)|*.docx";

    public Task<byte[]> ExportAsync(ExportContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        // HtmlConverter é síncrono. Mantemos em background para não bloquear a UI
        // caso o usuário exporte documentos maiores.
        return Task.Run(() => GenerateDocxBytes(context.Document));
    }

    private byte[] GenerateDocxBytes(EditorJsDocument document)
    {
        var html = htmlBuilder.Build(document, new HtmlBuildOptions
        {
            Variant = HtmlVariant.Standard,
            DocumentTitle = "Note Export"
        });

        using var stream = new MemoryStream();

        using (var wordDocument = WordprocessingDocument.Create(
            stream,
            DocumentFormat.OpenXml.WordprocessingDocumentType.Document))
        {
            var mainPart = wordDocument.AddMainDocumentPart();
            mainPart.Document = new Document(new Body());

            var converter = new HtmlConverter(mainPart)
            {
                // Mantém hyperlinks clicáveis em vez de descartar âncoras.
                SupportsAnchorLinks = true,

                // Mantém comportamento esperado para imagens externas absolutas.
                // data:image/* continua dependendo do suporte interno do HtmlToOpenXml.
                ImageProcessing = ImageProcessingMode.Embed
            };

            var body = mainPart.Document.Body;
            if (body is null)
                throw new InvalidOperationException("DOCX main document body was not initialized.");

            foreach (var element in converter.Parse(html))
                body.Append(element);

            mainPart.Document.Save();
        }

        return stream.ToArray();
    }
}
