using Ganss.Xss;
using MinimalTextEditorLite.Exporters.Contracts.EditorJs;
using System.Net;
using System.Text;
using System.Text.Json;

namespace MinimalTextEditorLite.Core.Exporters;

public sealed class HtmlExporter : IExporter
{
    private readonly HtmlSanitizer sanitizer = CreateSanitizer();

    public string Id => "html";
    public string DisplayName => "HTML";
    public string DefaultFileName => "Note.html";
    public string FileDialogFilter => "HTML Files (*.html)|*.html";

    public Task<byte[]> ExportAsync(ExportContext context)
    {
        var document = context.Document;
        var htmlBuilder = new StringBuilder();
        htmlBuilder.Append("<!DOCTYPE html>");
        htmlBuilder.Append("<html lang='en'>");
        htmlBuilder.Append("<head>");
        htmlBuilder.Append("<meta charset='UTF-8'>");
        htmlBuilder.Append("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        htmlBuilder.Append("<title>Note Export</title>");
        htmlBuilder.Append("<style>");
        htmlBuilder.Append("body { font-family: Arial, sans-serif; line-height: 1.6; }");
        htmlBuilder.Append("h1, h2, h3, h4, h5, h6 { margin: 10px 0; }");
        htmlBuilder.Append("p { margin: 10px 0; }");
        htmlBuilder.Append("ul, ol { margin: 10px 0; padding-left: 20px; }");
        htmlBuilder.Append("blockquote { margin: 10px 0; padding: 10px 20px; background: #f9f9f9; border-left: 4px solid #ccc; }");
        htmlBuilder.Append("code { font-family: 'Courier New', monospace; background: #f4f4f4; padding: 2px 4px; border-radius: 4px; }");
        htmlBuilder.Append("table { border-collapse: collapse; width: 100%; margin: 10px 0; }");
        htmlBuilder.Append("th, td { border: 1px solid #ccc; padding: 8px; text-align: left; }");
        htmlBuilder.Append("img { max-width: 100%; height: auto; margin: 10px 0; }");
        htmlBuilder.Append("</style>");
        htmlBuilder.Append("</head>");
        htmlBuilder.Append("<body>");

        foreach (var block in document.Blocks)
            AppendBlock(htmlBuilder, block);

        htmlBuilder.Append("</body>");
        htmlBuilder.Append("</html>");

        return Task.FromResult(Encoding.UTF8.GetBytes(htmlBuilder.ToString()));
    }

    private void AppendBlock(StringBuilder htmlBuilder, EditorJsBlock block)
    {
        switch (block.Type)
        {
            case "header":
            {
                var data = block.Data.Deserialize<EditorJsHeaderData>(EditorJsJson.Options);
                var level = Math.Clamp(data?.Level ?? 1, 1, 6);
                htmlBuilder.Append($"<h{level}>{SafeInlineHtml(data?.Text)}</h{level}>");
                break;
            }
            case "paragraph":
            {
                var data = block.Data.Deserialize<EditorJsParagraphData>(EditorJsJson.Options);
                htmlBuilder.Append($"<p>{SafeInlineHtml(data?.Text)}</p>");
                break;
            }
            case "list":
            {
                var data = block.Data.Deserialize<EditorJsListData>(EditorJsJson.Options);
                var listTag = string.Equals(data?.Style, "ordered", StringComparison.OrdinalIgnoreCase) ? "ol" : "ul";
                htmlBuilder.Append($"<{listTag}>");

                foreach (var item in data?.Items ?? [])
                    htmlBuilder.Append($"<li>{SafeInlineHtml(item)}</li>");

                htmlBuilder.Append($"</{listTag}>");
                break;
            }
            case "checklist":
            {
                var data = block.Data.Deserialize<EditorJsChecklistData>(EditorJsJson.Options);
                htmlBuilder.Append("<ul>");

                foreach (var item in data?.Items ?? [])
                {
                    var checkbox = item.Checked ? "&#9745;" : "&#9744;";
                    htmlBuilder.Append($"{checkbox} {SafeInlineHtml(item.Text)}<br>");
                }

                htmlBuilder.Append("</ul>");
                break;
            }
            case "quote":
            {
                var data = block.Data.Deserialize<EditorJsQuoteData>(EditorJsJson.Options);
                htmlBuilder.Append($"<blockquote><p>{SafeInlineHtml(data?.Text)}</p><footer>- {SafeInlineHtml(data?.Caption)}</footer></blockquote>");
                break;
            }
            case "warning":
            {
                var data = block.Data.Deserialize<EditorJsWarningData>(EditorJsJson.Options);
                htmlBuilder.Append("<div style='border: 1px solid #ffa500; padding: 10px; margin: 10px 0; background: #fff8e5;'>");
                htmlBuilder.Append($"<strong>{SafeInlineHtml(data?.Title)}</strong>: {SafeInlineHtml(data?.Message)}");
                htmlBuilder.Append("</div>");
                break;
            }
            case "code":
            {
                var data = block.Data.Deserialize<EditorJsCodeData>(EditorJsJson.Options);
                htmlBuilder.Append($"<pre><code>{SafeText(data?.Code)}</code></pre>");
                break;
            }
            case "delimiter":
            {
                htmlBuilder.Append("<p style='text-align:center; font-size:28px; margin:15px;'>***</p>");
                break;
            }
            case "table":
            {
                var data = block.Data.Deserialize<EditorJsTableData>(EditorJsJson.Options);
                htmlBuilder.Append("<table>");

                foreach (var row in data?.Content ?? [])
                {
                    htmlBuilder.Append("<tr>");

                    foreach (var cell in row)
                        htmlBuilder.Append($"<td>{SafeInlineHtml(cell)}</td>");

                    htmlBuilder.Append("</tr>");
                }

                htmlBuilder.Append("</table>");
                break;
            }
            case "image":
            {
                var data = block.Data.Deserialize<EditorJsImageData>(EditorJsJson.Options);
                htmlBuilder.Append("<figure>");
                htmlBuilder.Append($"<img src=\"{SafeText(data?.Url)}\" alt=\"{SafeText(data?.Caption)}\">");

                if (!string.IsNullOrEmpty(data?.Caption))
                    htmlBuilder.Append($"<figcaption>{SafeInlineHtml(data.Caption)}</figcaption>");

                htmlBuilder.Append("</figure>");
                break;
            }
        }
    }

    private static HtmlSanitizer CreateSanitizer()
    {
        var sanitizer = new HtmlSanitizer();

        sanitizer.AllowedTags.Clear();
        sanitizer.AllowedTags.Add("b");
        sanitizer.AllowedTags.Add("strong");
        sanitizer.AllowedTags.Add("i");
        sanitizer.AllowedTags.Add("em");
        sanitizer.AllowedTags.Add("u");
        sanitizer.AllowedTags.Add("a");
        sanitizer.AllowedTags.Add("code");
        sanitizer.AllowedTags.Add("mark");
        sanitizer.AllowedTags.Add("br");

        sanitizer.AllowedAttributes.Clear();
        sanitizer.AllowedAttributes.Add("href");
        sanitizer.AllowedAttributes.Add("title");
        sanitizer.AllowedAttributes.Add("target");
        sanitizer.AllowedAttributes.Add("rel");

        sanitizer.AllowedSchemes.Clear();
        sanitizer.AllowedSchemes.Add("http");
        sanitizer.AllowedSchemes.Add("https");
        sanitizer.AllowedSchemes.Add("mailto");

        return sanitizer;
    }

    private string SafeInlineHtml(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return sanitizer.Sanitize(value);
    }

    private static string SafeText(string? value)
    {
        return WebUtility.HtmlEncode(value ?? string.Empty);
    }
}
