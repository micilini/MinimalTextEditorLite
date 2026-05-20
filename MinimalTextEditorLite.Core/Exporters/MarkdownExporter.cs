using MinimalTextEditorLite.Core.Models;
using MinimalTextEditorLite.Exporters.Contracts.EditorJs;
using ReverseMarkdown;
using System.Net;
using System.Text;
using System.Text.Json;

namespace MinimalTextEditorLite.Core.Exporters;

public sealed class MarkdownExporter : IExporter
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

    private void AppendBlock(StringBuilder builder, EditorJsBlock block)
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
                AppendSeparated(builder, $"![{caption}]({data?.Url ?? string.Empty})");
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
}
