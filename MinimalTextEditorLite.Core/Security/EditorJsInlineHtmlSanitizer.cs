using Ganss.Xss;

namespace MinimalTextEditorLite.Core.Security;

public sealed class EditorJsInlineHtmlSanitizer
{
    private readonly HtmlSanitizer sanitizer = CreateSanitizer();

    public string SanitizeInlineHtml(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return sanitizer.Sanitize(value);
    }

    public string SanitizePlainText(string? value)
    {
        return value ?? string.Empty;
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
}
