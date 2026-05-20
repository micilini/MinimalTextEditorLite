using System.Collections.Generic;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace MinimalTextEditorLite.Exporters.Contracts.EditorJs;

public sealed class EditorJsDocument
{
    [JsonPropertyName("time")]
    public long? Time { get; set; }

    [JsonPropertyName("blocks")]
    public List<EditorJsBlock> Blocks { get; set; } = new();

    [JsonPropertyName("version")]
    public string? Version { get; set; }
}

public sealed class EditorJsBlock
{
    [JsonPropertyName("id")]
    public string? Id { get; set; }

    [JsonPropertyName("type")]
    public string Type { get; set; } = string.Empty;

    [JsonPropertyName("data")]
    public JsonElement Data { get; set; }
}

public static class EditorJsJson
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNameCaseInsensitive = true,
        WriteIndented = false
    };
}
