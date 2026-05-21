using MinimalTextEditorLite.Core.Models;

namespace MinimalTextEditorLite.Core.Exporters;

internal static class ExportMetadataHelper
{
    public static NoteMetadata FromNote(NoteModel note)
    {
        ArgumentNullException.ThrowIfNull(note);

        return new NoteMetadata
        {
            Title = Normalize(note.Title),
            Slug = Normalize(note.Slug),
            Tags = Normalize(note.Tags),
            PublishDate = note.PublishDate
        };
    }

    public static string GetDocumentTitle(NoteModel note)
    {
        return Normalize(note.Title) ?? "Note Export";
    }

    public static string[] SplitTags(string? tags)
    {
        if (string.IsNullOrWhiteSpace(tags))
            return [];

        return tags
            .Split(',', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static string? GetIsoDate(DateTime? date)
    {
        return date?.ToString("yyyy-MM-dd");
    }

    public static string? Normalize(string? value)
    {
        return string.IsNullOrWhiteSpace(value)
            ? null
            : value.Trim();
    }
}
