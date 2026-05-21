using MinimalTextEditorLite.Core.Exporters;
using MinimalTextEditorLite.Core.Models;
using System.Text.Json;

namespace MinimalTextEditorLite.Core.Importers;

public sealed class JsonImporter : IImporter
{
    public string Extension => ".json";

    public Task<ImportedNoteContent> ImportAsync(string content, string filePath)
    {
        ArgumentNullException.ThrowIfNull(content);

        try
        {
            using var jsonDocument = JsonDocument.Parse(content);
            var root = jsonDocument.RootElement;

            if (root.ValueKind == JsonValueKind.Object &&
                TryGetProperty(root, "document", out var documentElement) &&
                documentElement.ValueKind == JsonValueKind.Object)
            {
                return Task.FromResult(new ImportedNoteContent
                {
                    EditorJsJson = documentElement.GetRawText(),
                    Metadata = ReadMetadata(root)
                });
            }
        }
        catch (JsonException)
        {
        }

        return Task.FromResult(new ImportedNoteContent
        {
            EditorJsJson = content
        });
    }

    private static NoteMetadata? ReadMetadata(JsonElement root)
    {
        if (!TryGetProperty(root, "metadata", out var metadataElement) ||
            metadataElement.ValueKind != JsonValueKind.Object)
        {
            return null;
        }

        var metadata = new NoteMetadata
        {
            Title = ReadString(metadataElement, "title"),
            Slug = ReadString(metadataElement, "slug"),
            Tags = ReadTags(metadataElement),
            PublishDate = ReadDate(metadataElement)
        };

        return metadata.HasAnyValue ? metadata : null;
    }

    private static string? ReadString(JsonElement element, string name)
    {
        if (!TryGetProperty(element, name, out var property))
            return null;

        return property.ValueKind == JsonValueKind.String
            ? ExportMetadataHelper.Normalize(property.GetString())
            : null;
    }

    private static string? ReadTags(JsonElement element)
    {
        if (!TryGetProperty(element, "tags", out var property))
            return null;

        if (property.ValueKind == JsonValueKind.String)
            return ExportMetadataHelper.Normalize(property.GetString());

        if (property.ValueKind != JsonValueKind.Array)
            return null;

        var tags = property
            .EnumerateArray()
            .Where(item => item.ValueKind == JsonValueKind.String)
            .Select(item => ExportMetadataHelper.Normalize(item.GetString()))
            .Where(tag => !string.IsNullOrWhiteSpace(tag))
            .Cast<string>()
            .Distinct(StringComparer.OrdinalIgnoreCase)
            .ToArray();

        return tags.Length == 0
            ? null
            : string.Join(", ", tags);
    }

    private static DateTime? ReadDate(JsonElement element)
    {
        var dateText = ReadString(element, "publishDate") ?? ReadString(element, "date");

        if (string.IsNullOrWhiteSpace(dateText))
            return null;

        return DateTime.TryParse(dateText, out var date)
            ? date
            : null;
    }

    private static bool TryGetProperty(JsonElement element, string name, out JsonElement value)
    {
        foreach (var property in element.EnumerateObject())
        {
            if (string.Equals(property.Name, name, StringComparison.OrdinalIgnoreCase))
            {
                value = property.Value;
                return true;
            }
        }

        value = default;
        return false;
    }
}
