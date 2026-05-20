using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace MinimalTextEditorLite.Exporters.Contracts.EditorJs;

public sealed class EditorJsHeaderData
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("level")]
    public int Level { get; set; } = 1;
}

public sealed class EditorJsParagraphData
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }
}

public sealed class EditorJsListData
{
    [JsonPropertyName("style")]
    public string? Style { get; set; }

    [JsonPropertyName("items")]
    public List<string> Items { get; set; } = new();
}

public sealed class EditorJsChecklistData
{
    [JsonPropertyName("items")]
    public List<EditorJsChecklistItemData> Items { get; set; } = new();
}

public sealed class EditorJsChecklistItemData
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("checked")]
    public bool Checked { get; set; }
}

public sealed class EditorJsQuoteData
{
    [JsonPropertyName("text")]
    public string? Text { get; set; }

    [JsonPropertyName("caption")]
    public string? Caption { get; set; }
}

public sealed class EditorJsWarningData
{
    [JsonPropertyName("title")]
    public string? Title { get; set; }

    [JsonPropertyName("message")]
    public string? Message { get; set; }
}

public sealed class EditorJsCodeData
{
    [JsonPropertyName("code")]
    public string? Code { get; set; }
}

public sealed class EditorJsTableData
{
    [JsonPropertyName("content")]
    public List<List<string>> Content { get; set; } = new();
}

public sealed class EditorJsImageData
{
    [JsonPropertyName("url")]
    public string? Url { get; set; }

    [JsonPropertyName("caption")]
    public string? Caption { get; set; }

    [JsonPropertyName("width")]
    public int? Width { get; set; }

    [JsonPropertyName("height")]
    public int? Height { get; set; }

    [JsonPropertyName("fileName")]
    public string? FileName { get; set; }

    [JsonPropertyName("mimeType")]
    public string? MimeType { get; set; }

    [JsonPropertyName("size")]
    public long? Size { get; set; }
}
