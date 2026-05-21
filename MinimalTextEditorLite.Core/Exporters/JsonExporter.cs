using MinimalTextEditorLite.Exporters.Contracts.EditorJs;
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MinimalTextEditorLite.Core.Exporters;

public sealed class JsonExporter : IExporter
{
    private static readonly JsonSerializerOptions JsonOptions = new(EditorJsJson.Options)
    {
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public string Id => "json";

    public string DisplayName => "JSON";

    public string DefaultFileName => "Note.json";

    public string FileDialogFilter => "JSON Files (*.json)|*.json";

    public Task<byte[]> ExportAsync(ExportContext context)
    {
        ArgumentNullException.ThrowIfNull(context);

        var metadata = ExportMetadataHelper.FromNote(context.Note);
        var tags = ExportMetadataHelper.SplitTags(metadata.Tags);

        var payload = new JsonExportPackage
        {
            Format = "minimal-text-editor-lite",
            FormatVersion = "2.2.0",
            ExportedAt = DateTime.UtcNow,
            Metadata = metadata.HasAnyValue
                ? new JsonExportMetadata
                {
                    Title = metadata.Title,
                    Slug = metadata.Slug,
                    Tags = tags.Length > 0 ? tags : null,
                    PublishDate = ExportMetadataHelper.GetIsoDate(metadata.PublishDate)
                }
                : null,
            Document = context.Document
        };

        var json = JsonSerializer.Serialize(payload, JsonOptions);
        return Task.FromResult(Encoding.UTF8.GetBytes(json));
    }

    private sealed class JsonExportPackage
    {
        [JsonPropertyName("format")]
        public string Format { get; init; } = string.Empty;

        [JsonPropertyName("formatVersion")]
        public string FormatVersion { get; init; } = string.Empty;

        [JsonPropertyName("exportedAt")]
        public DateTime ExportedAt { get; init; }

        [JsonPropertyName("metadata")]
        public JsonExportMetadata? Metadata { get; init; }

        [JsonPropertyName("document")]
        public EditorJsDocument Document { get; init; } = new();
    }

    private sealed class JsonExportMetadata
    {
        [JsonPropertyName("title")]
        public string? Title { get; init; }

        [JsonPropertyName("slug")]
        public string? Slug { get; init; }

        [JsonPropertyName("tags")]
        public string[]? Tags { get; init; }

        [JsonPropertyName("publishDate")]
        public string? PublishDate { get; init; }
    }
}
