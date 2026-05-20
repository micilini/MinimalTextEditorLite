using Markdig;
using Markdig.Extensions.Tables;
using Markdig.Syntax;
using Markdig.Syntax.Inlines;
using MinimalTextEditorLite.Core.Models;
using MinimalTextEditorLite.Exporters.Contracts.EditorJs;
using System.Globalization;
using System.Net;
using System.Text;
using System.Text.Json;

namespace MinimalTextEditorLite.Core.Importers;

public sealed class MarkdownImporter : IImporter
{
    private const long MaxImageBytes = 50L * 1024 * 1024;

    private static readonly Dictionary<string, string> ImageMimeTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        [".png"] = "image/png",
        [".jpg"] = "image/jpeg",
        [".jpeg"] = "image/jpeg",
        [".gif"] = "image/gif",
        [".webp"] = "image/webp",
        [".svg"] = "image/svg+xml"
    };

    public string Extension => ".md";

    public Task<ImportedNoteContent> ImportAsync(string content, string filePath)
    {
        var (metadata, markdownBody) = SplitFrontMatter(content);
        var markdownDirectory = Path.GetDirectoryName(Path.GetFullPath(filePath)) ?? Directory.GetCurrentDirectory();
        var pipeline = new MarkdownPipelineBuilder()
            .UseAdvancedExtensions()
            .UsePipeTables()
            .UseTaskLists()
            .Build();

        var markdownDocument = Markdown.Parse(markdownBody, pipeline);
        var blocks = new List<EditorJsBlock>();

        foreach (var block in markdownDocument)
            AppendBlock(blocks, block, markdownDirectory);

        var editorDocument = new EditorJsDocument
        {
            Time = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            Version = "2.30.6",
            Blocks = blocks
        };

        return Task.FromResult(new ImportedNoteContent
        {
            EditorJsJson = JsonSerializer.Serialize(editorDocument, EditorJsJson.Options),
            Metadata = metadata
        });
    }

    private static void AppendBlock(List<EditorJsBlock> blocks, Block block, string markdownDirectory)
    {
        switch (block)
        {
            case HeadingBlock heading:
                blocks.Add(CreateBlock("header", new EditorJsHeaderData
                {
                    Text = RenderInline(heading.Inline),
                    Level = Math.Clamp(heading.Level, 1, 6)
                }));
                break;

            case ParagraphBlock paragraph:
                AppendParagraphBlock(blocks, paragraph, markdownDirectory);
                break;

            case ListBlock list:
                AppendListBlock(blocks, list);
                break;

            case QuoteBlock quote:
                blocks.Add(CreateBlock("quote", new EditorJsQuoteData
                {
                    Text = RenderBlocksAsInline(quote),
                    Caption = string.Empty
                }));
                break;

            case FencedCodeBlock fencedCode:
                blocks.Add(CreateBlock("code", new EditorJsCodeData { Code = fencedCode.Lines.ToString() }));
                break;

            case CodeBlock code:
                blocks.Add(CreateBlock("code", new EditorJsCodeData { Code = code.Lines.ToString() }));
                break;

            case ThematicBreakBlock:
                blocks.Add(CreateBlock("delimiter", new { }));
                break;

            case Table table:
                blocks.Add(CreateBlock("table", new EditorJsTableData { Content = ReadTable(table) }));
                break;

            case HtmlBlock:
                break;

            default:
                var text = RenderBlocksAsInline(block);
                if (!string.IsNullOrWhiteSpace(text))
                    blocks.Add(CreateBlock("paragraph", new EditorJsParagraphData { Text = text }));
                break;
        }
    }

    private static void AppendParagraphBlock(List<EditorJsBlock> blocks, ParagraphBlock paragraph, string markdownDirectory)
    {
        if (TryGetSingleImage(paragraph.Inline, markdownDirectory, out var image))
        {
            blocks.Add(CreateBlock("image", new EditorJsImageData
            {
                Url = image.Url,
                Caption = image.Caption
            }));
            return;
        }

        blocks.Add(CreateBlock("paragraph", new EditorJsParagraphData { Text = RenderInline(paragraph.Inline) }));
    }

    private static void AppendListBlock(List<EditorJsBlock> blocks, ListBlock list)
    {
        var renderedItems = new List<string>();
        var checklistItems = new List<EditorJsChecklistItemData>();
        var isChecklist = true;

        foreach (var item in list.OfType<ListItemBlock>())
        {
            var itemText = RenderBlocksAsInline(item).Trim();
            var checklistItem = ParseChecklistItem(itemText);
            if (checklistItem == null)
                isChecklist = false;
            else
                checklistItems.Add(checklistItem);

            renderedItems.Add(itemText);
        }

        if (isChecklist && checklistItems.Count > 0)
        {
            blocks.Add(CreateBlock("checklist", new EditorJsChecklistData { Items = checklistItems }));
            return;
        }

        blocks.Add(CreateBlock("list", new EditorJsListData
        {
            Style = list.IsOrdered ? "ordered" : "unordered",
            Items = renderedItems
        }));
    }

    private static EditorJsChecklistItemData? ParseChecklistItem(string itemText)
    {
        if (itemText.StartsWith("[ ] ", StringComparison.Ordinal))
            return new EditorJsChecklistItemData { Text = itemText[4..], Checked = false };

        if (itemText.StartsWith("[x] ", StringComparison.OrdinalIgnoreCase))
            return new EditorJsChecklistItemData { Text = itemText[4..], Checked = true };

        return null;
    }

    private static List<List<string>> ReadTable(Table table)
    {
        var rows = new List<List<string>>();

        foreach (var row in table.OfType<TableRow>())
        {
            var cells = new List<string>();

            foreach (var cell in row.OfType<TableCell>())
                cells.Add(RenderBlocksAsInline(cell));

            rows.Add(cells);
        }

        return rows;
    }

    private static string RenderBlocksAsInline(ContainerBlock container)
    {
        var builder = new StringBuilder();

        foreach (var block in container)
        {
            if (block is LeafBlock leaf)
            {
                if (builder.Length > 0)
                    builder.Append("<br>");

                builder.Append(RenderInline(leaf.Inline));
            }
            else if (block is ContainerBlock child)
            {
                if (builder.Length > 0)
                    builder.Append("<br>");

                builder.Append(RenderBlocksAsInline(child));
            }
        }

        return builder.ToString();
    }

    private static string RenderBlocksAsInline(Block block)
    {
        if (block is ContainerBlock container)
            return RenderBlocksAsInline(container);

        if (block is LeafBlock leaf)
            return RenderInline(leaf.Inline);

        return WebUtility.HtmlEncode(block.ToString());
    }

    private static string RenderInline(ContainerInline? inline)
    {
        if (inline == null)
            return string.Empty;

        var builder = new StringBuilder();

        foreach (var child in inline)
            builder.Append(RenderInlineObject(child));

        return builder.ToString();
    }

    private static string RenderInlineObject(Inline inline)
    {
        switch (inline)
        {
            case LiteralInline literal:
                return WebUtility.HtmlEncode(literal.Content.ToString());

            case LineBreakInline:
                return "<br>";

            case CodeInline code:
                return $"<code>{WebUtility.HtmlEncode(code.Content)}</code>";

            case EmphasisInline emphasis:
            {
                var tag = emphasis.DelimiterCount >= 2 ? "b" : "i";
                return $"<{tag}>{RenderInline(emphasis)}</{tag}>";
            }

            case LinkInline link when link.IsImage:
                return WebUtility.HtmlEncode(GetPlainText(link));

            case LinkInline link:
            {
                var text = RenderInline(link);
                var url = link.Url ?? string.Empty;

                if (url.TrimStart().StartsWith("javascript:", StringComparison.OrdinalIgnoreCase))
                    return text;

                return $"<a href=\"{WebUtility.HtmlEncode(url)}\">{text}</a>";
            }

            case ContainerInline container:
                return RenderInline(container);

            default:
                return WebUtility.HtmlEncode(inline.ToString());
        }
    }

    private static bool TryGetSingleImage(ContainerInline? inline, string markdownDirectory, out MarkdownImageSource imageSource)
    {
        imageSource = new MarkdownImageSource(string.Empty, string.Empty);

        if (inline == null)
            return false;

        var children = inline.Where(child => child is not LineBreakInline).ToList();
        if (children.Count != 1 || children[0] is not LinkInline { IsImage: true } image)
            return false;

        var originalUrl = image.Url ?? string.Empty;
        var caption = GetPlainText(image);
        var resolvedUrl = ResolveMarkdownImageUrl(originalUrl, markdownDirectory);
        imageSource = new MarkdownImageSource(resolvedUrl, caption);
        return true;
    }

    private static string ResolveMarkdownImageUrl(string url, string markdownDirectory)
    {
        if (string.IsNullOrWhiteSpace(url))
            return url;

        if (url.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            return url;

        if (Path.IsPathRooted(url))
            return TryConvertLocalImageToDataUrl(url) ?? url;

        if (Uri.TryCreate(url, UriKind.Absolute, out var absoluteUri))
        {
            if (absoluteUri.Scheme is "http" or "https")
                return url;

            if (absoluteUri.Scheme.Equals("file", StringComparison.OrdinalIgnoreCase))
                return TryConvertLocalImageToDataUrl(absoluteUri.LocalPath) ?? url;

            return url;
        }

        var localPath = Path.GetFullPath(Path.Combine(markdownDirectory, url.Replace('/', Path.DirectorySeparatorChar)));

        return TryConvertLocalImageToDataUrl(localPath) ?? url;
    }

    private static string? TryConvertLocalImageToDataUrl(string path)
    {
        try
        {
            if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
                return null;

            var extension = Path.GetExtension(path);
            if (!ImageMimeTypes.TryGetValue(extension, out var mimeType))
                return null;

            var info = new FileInfo(path);
            if (info.Length > MaxImageBytes)
                return null;

            var bytes = File.ReadAllBytes(path);
            return $"data:{mimeType};base64,{Convert.ToBase64String(bytes)}";
        }
        catch
        {
            return null;
        }
    }

    private static string GetPlainText(ContainerInline inline)
    {
        var builder = new StringBuilder();

        foreach (var child in inline)
        {
            switch (child)
            {
                case LiteralInline literal:
                    builder.Append(literal.Content);
                    break;
                case CodeInline code:
                    builder.Append(code.Content);
                    break;
                case ContainerInline container:
                    builder.Append(GetPlainText(container));
                    break;
            }
        }

        return builder.ToString();
    }

    private static EditorJsBlock CreateBlock<T>(string type, T data)
    {
        return new EditorJsBlock
        {
            Id = Guid.NewGuid().ToString("N")[..10],
            Type = type,
            Data = JsonSerializer.SerializeToElement(data, EditorJsJson.Options)
        };
    }

    private static (NoteMetadata? Metadata, string Body) SplitFrontMatter(string content)
    {
        var normalized = content.Replace("\r\n", "\n");

        if (!normalized.StartsWith("---\n", StringComparison.Ordinal))
            return (null, content);

        var end = normalized.IndexOf("\n---", 4, StringComparison.Ordinal);
        if (end < 0)
            return (null, content);

        var afterEnd = end + 4;
        if (afterEnd < normalized.Length && normalized[afterEnd] == '\n')
            afterEnd++;

        var frontMatter = normalized[4..end];
        var metadata = ParseMetadata(frontMatter);
        return (metadata, normalized[afterEnd..]);
    }

    private static NoteMetadata ParseMetadata(string frontMatter)
    {
        string? title = null;
        string? slug = null;
        string? tags = null;
        DateTime? publishDate = null;

        foreach (var line in frontMatter.Split('\n'))
        {
            var separatorIndex = line.IndexOf(':');
            if (separatorIndex <= 0)
                continue;

            var key = line[..separatorIndex].Trim();
            var value = line[(separatorIndex + 1)..].Trim().Trim('"');

            switch (key.ToLowerInvariant())
            {
                case "title":
                    title = value;
                    break;
                case "slug":
                    slug = value;
                    break;
                case "tags":
                    tags = NormalizeTags(value);
                    break;
                case "date":
                    if (DateTime.TryParse(value, CultureInfo.InvariantCulture, DateTimeStyles.AssumeLocal, out var date))
                        publishDate = date;
                    break;
            }
        }

        return new NoteMetadata
        {
            Title = title,
            Slug = slug,
            Tags = tags,
            PublishDate = publishDate
        };
    }

    private static string NormalizeTags(string value)
    {
        var trimmed = value.Trim();
        if (trimmed.StartsWith("[", StringComparison.Ordinal) && trimmed.EndsWith("]", StringComparison.Ordinal))
            trimmed = trimmed[1..^1];

        return string.Join(", ",
            trimmed.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(tag => tag.Trim('"')));
    }

    private sealed record MarkdownImageSource(string Url, string Caption);
}
