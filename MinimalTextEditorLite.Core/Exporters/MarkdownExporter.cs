using MinimalTextEditorLite.Core.Models;
using MinimalTextEditorLite.Exporters.Contracts.EditorJs;
using ReverseMarkdown;
using System.Net;
using System.Text;
using System.Text.Json;

namespace MinimalTextEditorLite.Core.Exporters;

public sealed class MarkdownExporter : IExporter, IMarkdownAssetExporter
{
    private readonly Converter inlineConverter = new(new Config
    {
        UnknownTags = Config.UnknownTagsOption.Bypass,
        GithubFlavored = true,
        RemoveComments = true,
        SmartHrefHandling = true
    });

    public string Id => "md";
    public string DisplayName => "Markdown";
    public string DefaultFileName => "Note.md";
    public string FileDialogFilter => "Markdown Files (*.md)|*.md";

    public Task<byte[]> ExportAsync(ExportContext context)
    {
        var builder = new StringBuilder();

        if (context.Settings.ExportFrontMatterYaml)
            AppendFrontMatter(builder, context.Note);

        foreach (var block in context.Document.Blocks)
            AppendBlock(builder, block);

        return Task.FromResult(Encoding.UTF8.GetBytes(builder.ToString().TrimEnd() + Environment.NewLine));
    }

    public async Task<MarkdownAssetExportResult> ExportWithAssetsAsync(ExportContext context, string outputDirectory)
    {
        if (string.IsNullOrWhiteSpace(outputDirectory))
            throw new ArgumentException("Output directory is required.", nameof(outputDirectory));

        Directory.CreateDirectory(outputDirectory);

        var baseName = GetSafeBaseFileName(context.Note);
        var uniqueBaseName = GetUniqueBaseName(outputDirectory, baseName);

        var markdownFilePath = Path.Combine(outputDirectory, uniqueBaseName + ".md");
        var assetsDirectoryName = uniqueBaseName + ".assets";
        var assetsDirectoryPath = Path.Combine(outputDirectory, assetsDirectoryName);
        var assetsDirectoryCreated = false;
        var assetIndex = 1;

        var builder = new StringBuilder();

        if (context.Settings.ExportFrontMatterYaml)
            AppendFrontMatter(builder, context.Note);

        var assetContext = new MarkdownAssetContext(
            assetsDirectoryPath,
            assetsDirectoryName,
            () =>
            {
                if (!assetsDirectoryCreated)
                {
                    Directory.CreateDirectory(assetsDirectoryPath);
                    assetsDirectoryCreated = true;
                }

                return assetIndex++;
            });

        foreach (var block in context.Document.Blocks)
            AppendBlock(builder, block, assetContext);

        await File.WriteAllTextAsync(markdownFilePath, builder.ToString().TrimEnd() + Environment.NewLine, Encoding.UTF8);

        return new MarkdownAssetExportResult
        {
            MarkdownFilePath = markdownFilePath,
            AssetsDirectoryPath = assetsDirectoryPath,
            AssetsWritten = Directory.Exists(assetsDirectoryPath)
                ? Directory.GetFiles(assetsDirectoryPath).Length
                : 0
        };
    }

    private void AppendBlock(StringBuilder builder, EditorJsBlock block)
    {
        AppendBlock(builder, block, assetContext: null);
    }

    private void AppendBlock(StringBuilder builder, EditorJsBlock block, MarkdownAssetContext? assetContext)
    {
        switch (block.Type)
        {
            case "header":
            {
                var data = block.Data.Deserialize<EditorJsHeaderData>(EditorJsJson.Options);
                var level = Math.Clamp(data?.Level ?? 1, 1, 6);
                AppendSeparated(builder, $"{new string('#', level)} {ConvertInline(data?.Text)}");
                break;
            }
            case "paragraph":
            {
                var data = block.Data.Deserialize<EditorJsParagraphData>(EditorJsJson.Options);
                AppendSeparated(builder, ConvertInline(data?.Text));
                break;
            }
            case "list":
            {
                var data = block.Data.Deserialize<EditorJsListData>(EditorJsJson.Options);
                var ordered = string.Equals(data?.Style, "ordered", StringComparison.OrdinalIgnoreCase);
                var index = 1;
                var lines = new List<string>();

                foreach (var item in data?.Items ?? [])
                    lines.Add(ordered ? $"{index++}. {ConvertInline(item)}" : $"- {ConvertInline(item)}");

                AppendSeparated(builder, string.Join(Environment.NewLine, lines));
                break;
            }
            case "checklist":
            {
                var data = block.Data.Deserialize<EditorJsChecklistData>(EditorJsJson.Options);
                var lines = (data?.Items ?? [])
                    .Select(item => $"- [{(item.Checked ? "x" : " ")}] {ConvertInline(item.Text)}");
                AppendSeparated(builder, string.Join(Environment.NewLine, lines));
                break;
            }
            case "quote":
            {
                var data = block.Data.Deserialize<EditorJsQuoteData>(EditorJsJson.Options);
                var quoteLines = ConvertInline(data?.Text)
                    .Split(["\r\n", "\n"], StringSplitOptions.None)
                    .Select(line => $"> {line}");

                var quote = string.Join(Environment.NewLine, quoteLines);
                if (!string.IsNullOrWhiteSpace(data?.Caption))
                    quote += $"{Environment.NewLine}>{Environment.NewLine}> - {ConvertInline(data.Caption)}";

                AppendSeparated(builder, quote);
                break;
            }
            case "warning":
            {
                var data = block.Data.Deserialize<EditorJsWarningData>(EditorJsJson.Options);
                AppendSeparated(builder, $"> **{ConvertInline(data?.Title)}**{Environment.NewLine}>{Environment.NewLine}> {ConvertInline(data?.Message)}");
                break;
            }
            case "code":
            {
                var data = block.Data.Deserialize<EditorJsCodeData>(EditorJsJson.Options);
                AppendSeparated(builder, $"```{Environment.NewLine}{data?.Code ?? string.Empty}{Environment.NewLine}```");
                break;
            }
            case "delimiter":
                AppendSeparated(builder, "---");
                break;

            case "table":
            {
                var data = block.Data.Deserialize<EditorJsTableData>(EditorJsJson.Options);
                AppendTable(builder, data?.Content ?? []);
                break;
            }
            case "image":
            {
                var data = block.Data.Deserialize<EditorJsImageData>(EditorJsJson.Options);
                var caption = ConvertInline(data?.Caption);
                var imageUrl = ResolveImageUrlForMarkdown(data, assetContext);
                AppendSeparated(builder, $"![{caption}]({imageUrl})");
                break;
            }
        }
    }

    private void AppendTable(StringBuilder builder, List<List<string>> rows)
    {
        if (rows.Count == 0)
            return;

        var columnCount = rows.Max(row => row.Count);
        if (columnCount == 0)
            return;

        var normalizedRows = rows
            .Select(row => row.Concat(Enumerable.Repeat(string.Empty, columnCount - row.Count)).ToList())
            .ToList();

        var tableBuilder = new StringBuilder();
        AppendTableRow(tableBuilder, normalizedRows[0]);
        AppendTableRow(tableBuilder, Enumerable.Repeat("---", columnCount).ToList());

        foreach (var row in normalizedRows.Skip(1))
            AppendTableRow(tableBuilder, row);

        AppendSeparated(builder, tableBuilder.ToString().TrimEnd());
    }

    private void AppendTableRow(StringBuilder builder, IReadOnlyList<string> row)
    {
        builder.Append("| ");
        builder.Append(string.Join(" | ", row.Select(cell => ConvertInline(cell).Replace("|", "\\|"))));
        builder.AppendLine(" |");
    }

    private static void AppendFrontMatter(StringBuilder builder, NoteModel note)
    {
        if (string.IsNullOrWhiteSpace(note.Title) &&
            string.IsNullOrWhiteSpace(note.Slug) &&
            string.IsNullOrWhiteSpace(note.Tags) &&
            note.PublishDate == null)
        {
            return;
        }

        builder.AppendLine("---");

        if (!string.IsNullOrWhiteSpace(note.Title))
            builder.AppendLine($"title: {YamlEscape(note.Title)}");

        if (!string.IsNullOrWhiteSpace(note.Slug))
            builder.AppendLine($"slug: {YamlEscape(note.Slug)}");

        if (!string.IsNullOrWhiteSpace(note.Tags))
            builder.AppendLine($"tags: [{FormatTags(note.Tags)}]");

        if (note.PublishDate.HasValue)
            builder.AppendLine($"date: {note.PublishDate.Value:yyyy-MM-dd}");

        builder.AppendLine("---");
        builder.AppendLine();
    }

    private string ConvertInline(string? value)
    {
        if (string.IsNullOrWhiteSpace(value))
            return string.Empty;

        return WebUtility.HtmlDecode(inlineConverter.Convert(value)).Trim();
    }

    private static void AppendSeparated(StringBuilder builder, string markdown)
    {
        if (string.IsNullOrWhiteSpace(markdown))
            return;

        if (builder.Length > 0 && !builder.ToString().EndsWith($"{Environment.NewLine}{Environment.NewLine}", StringComparison.Ordinal))
            builder.AppendLine();

        builder.AppendLine(markdown);
        builder.AppendLine();
    }

    private static string FormatTags(string tags)
    {
        return string.Join(", ",
            tags.Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
                .Select(YamlEscape));
    }

    private static string YamlEscape(string value)
    {
        var trimmed = value.Trim();
        var needsQuotes = trimmed.Any(char.IsControl) ||
            trimmed.Contains(':') ||
            trimmed.Contains('#') ||
            trimmed.Contains('[') ||
            trimmed.Contains(']') ||
            trimmed.Contains('{') ||
            trimmed.Contains('}') ||
            trimmed.Contains('"') ||
            trimmed.Contains('\n') ||
            trimmed.Contains('\r');

        return needsQuotes ? $"\"{trimmed.Replace("\"", "\\\"")}\"" : trimmed;
    }

    private string ResolveImageUrlForMarkdown(EditorJsImageData? data, MarkdownAssetContext? assetContext)
    {
        var url = data?.Url;

        if (string.IsNullOrWhiteSpace(url))
            return string.Empty;

        if (assetContext == null)
            return url;

        if (url.StartsWith("http://", StringComparison.OrdinalIgnoreCase) ||
            url.StartsWith("https://", StringComparison.OrdinalIgnoreCase))
        {
            return url;
        }

        if (!TryParseDataImage(url, out var mimeType, out var bytes))
            return url;

        var extension = GetExtensionFromMimeType(mimeType);
        var fileName = GetSafeAssetFileName(data?.FileName, assetContext.NextIndex(), extension);
        var filePath = Path.Combine(assetContext.AssetsDirectoryPath, fileName);

        File.WriteAllBytes(filePath, bytes);

        return $"{assetContext.AssetsDirectoryName}/{fileName}";
    }

    private static bool TryParseDataImage(string url, out string mimeType, out byte[] bytes)
    {
        mimeType = string.Empty;
        bytes = [];

        if (!url.StartsWith("data:image/", StringComparison.OrdinalIgnoreCase))
            return false;

        var commaIndex = url.IndexOf(',');
        if (commaIndex < 0)
            return false;

        var metadata = url[..commaIndex];
        var payload = url[(commaIndex + 1)..];
        var metadataParts = metadata.Split(';', StringSplitOptions.RemoveEmptyEntries);
        if (metadataParts.Length < 2 || !metadataParts.Any(part => part.Equals("base64", StringComparison.OrdinalIgnoreCase)))
            return false;

        mimeType = metadataParts[0]["data:".Length..];

        try
        {
            bytes = Convert.FromBase64String(payload);
            return true;
        }
        catch
        {
            bytes = [];
            return false;
        }
    }

    private static string GetExtensionFromMimeType(string mimeType)
    {
        return mimeType.ToLowerInvariant() switch
        {
            "image/png" => ".png",
            "image/jpeg" => ".jpg",
            "image/gif" => ".gif",
            "image/webp" => ".webp",
            "image/svg+xml" => ".svg",
            _ => ".img"
        };
    }

    private static string GetSafeAssetFileName(string? originalFileName, int index, string extension)
    {
        var fallback = $"image-{index:000}{extension}";

        if (string.IsNullOrWhiteSpace(originalFileName))
            return fallback;

        var nameWithoutExtension = Path.GetFileNameWithoutExtension(originalFileName);
        var originalExtension = Path.GetExtension(originalFileName);

        var finalExtension = string.IsNullOrWhiteSpace(originalExtension)
            ? extension
            : originalExtension;

        var invalidChars = Path.GetInvalidFileNameChars();
        var safeName = new string(nameWithoutExtension
            .Select(ch => invalidChars.Contains(ch) ? '-' : ch)
            .ToArray())
            .Trim('-', ' ', '.');

        if (string.IsNullOrWhiteSpace(safeName))
            return fallback;

        return $"{index:000}-{safeName}{finalExtension}";
    }

    private static string GetSafeBaseFileName(NoteModel note)
    {
        var candidate =
            !string.IsNullOrWhiteSpace(note.Slug) ? note.Slug :
            !string.IsNullOrWhiteSpace(note.Title) ? note.Title :
            "Note";

        var invalidChars = Path.GetInvalidFileNameChars();
        var safe = new string(candidate
            .Select(ch => invalidChars.Contains(ch) ? '-' : ch)
            .ToArray())
            .Trim('-', ' ', '.');

        return string.IsNullOrWhiteSpace(safe) ? "Note" : safe;
    }

    private static string GetUniqueBaseName(string outputDirectory, string baseName)
    {
        var candidate = baseName;
        var index = 1;

        while (File.Exists(Path.Combine(outputDirectory, candidate + ".md")) ||
               Directory.Exists(Path.Combine(outputDirectory, candidate + ".assets")))
        {
            candidate = $"{baseName}-{index++}";
        }

        return candidate;
    }

    private sealed class MarkdownAssetContext
    {
        public MarkdownAssetContext(string assetsDirectoryPath, string assetsDirectoryName, Func<int> nextIndex)
        {
            AssetsDirectoryPath = assetsDirectoryPath;
            AssetsDirectoryName = assetsDirectoryName;
            NextIndex = nextIndex;
        }

        public string AssetsDirectoryPath { get; }
        public string AssetsDirectoryName { get; }
        public Func<int> NextIndex { get; }
    }
}
