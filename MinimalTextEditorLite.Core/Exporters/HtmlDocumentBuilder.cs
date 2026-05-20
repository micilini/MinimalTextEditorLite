using Ganss.Xss;
using MinimalTextEditorLite.Exporters.Contracts.EditorJs;
using System.Net;
using System.Text;
using System.Text.Json;

namespace MinimalTextEditorLite.Core.Exporters;

public sealed class HtmlDocumentBuilder
{
    private readonly HtmlSanitizer sanitizer = CreateSanitizer();

    public string Build(EditorJsDocument document, HtmlBuildOptions options)
    {
        ArgumentNullException.ThrowIfNull(document);
        ArgumentNullException.ThrowIfNull(options);

        var htmlBuilder = new StringBuilder();
        htmlBuilder.Append("<!DOCTYPE html>");
        htmlBuilder.Append("<html lang='en'>");
        htmlBuilder.Append("<head>");
        htmlBuilder.Append("<meta charset='UTF-8'>");
        htmlBuilder.Append("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
        htmlBuilder.Append($"<title>{SafeText(options.DocumentTitle)}</title>");
        htmlBuilder.Append("<style>");
        AppendCss(htmlBuilder, options.Variant);
        htmlBuilder.Append("</style>");
        htmlBuilder.Append("</head>");
        htmlBuilder.Append("<body>");

        foreach (var block in document.Blocks)
            AppendBlock(htmlBuilder, block, options.Variant);

        if (options.Variant == HtmlVariant.Print)
            htmlBuilder.Append(BuildReadyScript());

        htmlBuilder.Append("</body>");
        htmlBuilder.Append("</html>");

        return htmlBuilder.ToString();
    }

    private static void AppendCss(StringBuilder htmlBuilder, HtmlVariant variant)
    {
        AppendStandardCss(htmlBuilder);

        if (variant == HtmlVariant.Print)
            AppendPrintCss(htmlBuilder);
    }

    private static void AppendStandardCss(StringBuilder htmlBuilder)
    {
        htmlBuilder.Append("body { font-family: 'Segoe UI', Arial, sans-serif; line-height: 1.6; color: #1a1a1a; }");
        htmlBuilder.Append("h1, h2, h3, h4, h5, h6 { margin: 16px 0 8px; color: #111; font-weight: 700; }");
        htmlBuilder.Append("h1 { font-size: 28px; }");
        htmlBuilder.Append("h2 { font-size: 22px; }");
        htmlBuilder.Append("h3 { font-size: 18px; }");
        htmlBuilder.Append("h4 { font-size: 16px; }");
        htmlBuilder.Append("h5 { font-size: 14px; }");
        htmlBuilder.Append("h6 { font-size: 13px; text-transform: uppercase; letter-spacing: .04em; }");
        htmlBuilder.Append("p { margin: 10px 0; font-size: 14px; }");
        htmlBuilder.Append("ul, ol { margin: 10px 0; padding-left: 24px; }");
        htmlBuilder.Append("li { margin: 4px 0; }");
        htmlBuilder.Append("blockquote { margin: 12px 0; padding: 12px 16px; background: #f5f5f5; border-left: 4px solid #999; font-style: italic; }");
        htmlBuilder.Append("blockquote p { margin: 0 0 6px; }");
        htmlBuilder.Append("blockquote footer { color: #666; font-size: 13px; font-style: normal; }");
        htmlBuilder.Append("code { font-family: 'Cascadia Code', 'Consolas', monospace; background: #f4f4f4; padding: 2px 6px; border-radius: 4px; font-size: .92em; }");
        htmlBuilder.Append("pre { font-family: 'Cascadia Code', 'Consolas', monospace; background: #1e1e1e; color: #d4d4d4; padding: 14px; border-radius: 6px; overflow-x: auto; white-space: pre-wrap; word-break: break-word; }");
        htmlBuilder.Append("pre code { background: transparent; color: inherit; padding: 0; border-radius: 0; }");
        htmlBuilder.Append("table { border-collapse: collapse; width: 100%; margin: 14px 0; }");
        htmlBuilder.Append("th, td { border: 1px solid #ccc; padding: 8px 10px; text-align: left; vertical-align: top; }");
        htmlBuilder.Append("th { background: #f0f0f0; font-weight: 600; }");
        htmlBuilder.Append("img { max-width: 100%; height: auto; margin: 10px 0; }");
        htmlBuilder.Append("figure { margin: 16px 0; text-align: center; }");
        htmlBuilder.Append("figure img { display: inline-block; margin: 0 auto; }");
        htmlBuilder.Append("figcaption { margin-top: 6px; color: #666; font-size: 13px; font-style: italic; }");
        htmlBuilder.Append("a { color: #1a4ba8; text-decoration: underline; }");
        htmlBuilder.Append(".mte-warning { border: 1px solid #f0a020; background: #fff8e5; padding: 12px 16px; border-radius: 4px; margin: 12px 0; }");
        htmlBuilder.Append(".mte-warning strong { color: #7a4b00; }");
        htmlBuilder.Append(".mte-delimiter { text-align: center; font-size: 22px; letter-spacing: 8px; margin: 20px 0; color: #999; }");
    }

    private static void AppendPrintCss(StringBuilder htmlBuilder)
    {
        htmlBuilder.Append("@page { size: A4; margin: 1.5cm; }");
        htmlBuilder.Append("html, body { -webkit-print-color-adjust: exact; print-color-adjust: exact; }");
        htmlBuilder.Append("body { font-family: 'Segoe UI Variable', 'Segoe UI', 'Segoe UI Emoji', Arial, sans-serif; font-size: 11pt; line-height: 1.55; }");
        htmlBuilder.Append("h1, h2, h3, h4, h5, h6 { page-break-after: avoid; break-after: avoid; }");
        htmlBuilder.Append("h1 { font-size: 24pt; }");
        htmlBuilder.Append("h2 { font-size: 19pt; }");
        htmlBuilder.Append("h3 { font-size: 15pt; }");
        htmlBuilder.Append("p, li { orphans: 3; widows: 3; }");
        htmlBuilder.Append("table, figure, pre, blockquote, .mte-warning { page-break-inside: avoid; break-inside: avoid; }");
        htmlBuilder.Append("img { page-break-inside: avoid; break-inside: avoid; }");
        htmlBuilder.Append("pre { white-space: pre-wrap; word-break: break-word; }");
        htmlBuilder.Append("a { color: #1a4ba8; text-decoration: underline; }");
        htmlBuilder.Append(".no-print { display: none; }");
    }

    private void AppendBlock(StringBuilder htmlBuilder, EditorJsBlock block, HtmlVariant variant)
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

                if (variant == HtmlVariant.Print)
                    htmlBuilder.Append("<div class=\"mte-warning\">");
                else
                    htmlBuilder.Append("<div style='border: 1px solid #ffa500; padding: 10px; margin: 10px 0; background: #fff8e5;'>");

                htmlBuilder.Append($"<strong>{SafeInlineHtml(data?.Title)}</strong>: {SafeInlineHtml(data?.Message)}");
                htmlBuilder.Append("</div>");
                break;
            }
            case "code":
            {
                var data = block.Data.Deserialize<EditorJsCodeData>(EditorJsJson.Options);
                var preStyle = variant == HtmlVariant.Standard
                    ? " style=\"font-family:'Cascadia Code','Consolas',monospace;\""
                    : string.Empty;

                htmlBuilder.Append($"<pre{preStyle}><code>{SafeText(data?.Code)}</code></pre>");
                break;
            }
            case "delimiter":
            {
                if (variant == HtmlVariant.Standard)
                    htmlBuilder.Append("<hr>");
                else
                    htmlBuilder.Append("<div class=\"mte-delimiter\">***</div>");

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
                AppendImageBlock(htmlBuilder, data, variant);
                break;
            }
        }
    }

    private void AppendImageBlock(StringBuilder htmlBuilder, EditorJsImageData? data, HtmlVariant variant)
    {
        var url = SafeText(data?.Url);
        var captionText = data?.Caption;
        var captionHtml = SafeInlineHtml(captionText);
        var captionAttribute = SafeText(captionText);

        if (variant == HtmlVariant.Standard)
        {
            htmlBuilder.Append($"<p style=\"text-align:center;\"><img src=\"{url}\" alt=\"{captionAttribute}\"></p>");

            if (!string.IsNullOrWhiteSpace(captionText))
                htmlBuilder.Append($"<p style=\"text-align:center;\"><em>{captionHtml}</em></p>");

            return;
        }

        htmlBuilder.Append("<figure>");
        htmlBuilder.Append($"<img src=\"{url}\" alt=\"{captionAttribute}\">");

        if (!string.IsNullOrWhiteSpace(captionText))
            htmlBuilder.Append($"<figcaption>{captionHtml}</figcaption>");

        htmlBuilder.Append("</figure>");
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

    private static string BuildReadyScript()
    {
        return """
<script>
(function () {
  var didSignal = false;

  function signalReady() {
    if (didSignal) {
      return;
    }

    didSignal = true;

    if (window.chrome && window.chrome.webview) {
      window.chrome.webview.postMessage('mte:print-ready');
    }
  }

  function waitForImages() {
    var imgs = Array.from(document.images);

    if (imgs.length === 0) {
      signalReady();
      return;
    }

    var pending = imgs.length;

    function decrement() {
      pending -= 1;

      if (pending <= 0) {
        signalReady();
      }
    }

    imgs.forEach(function (img) {
      if (img.complete) {
        decrement();
        return;
      }

      img.addEventListener('load', decrement, { once: true });
      img.addEventListener('error', decrement, { once: true });
    });

    window.setTimeout(signalReady, 10000);
  }

  if (document.readyState === 'complete') {
    waitForImages();
  } else {
    window.addEventListener('load', waitForImages, { once: true });
    window.setTimeout(waitForImages, 3000);
  }
})();
</script>
""";
    }
}
