using System.Text;

namespace MinimalTextEditorLite.Core.Exporters;

public sealed class HtmlExporter : IExporter
{
    private readonly HtmlDocumentBuilder builder;

    public HtmlExporter(HtmlDocumentBuilder builder)
    {
        this.builder = builder;
    }

    public string Id => "html";
    public string DisplayName => "HTML";
    public string DefaultFileName => "Note.html";
    public string FileDialogFilter => "HTML Files (*.html)|*.html";

    public Task<byte[]> ExportAsync(ExportContext context)
    {
        var html = builder.Build(context.Document, new HtmlBuildOptions
        {
            Variant = HtmlVariant.Standard,
            DocumentTitle = "Note Export"
        });

        return Task.FromResult(Encoding.UTF8.GetBytes(html));
    }
}
