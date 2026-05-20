namespace MinimalTextEditorLite.Core.Models;

public sealed class NoteMetadata
{
    public string? Title { get; init; }
    public string? Slug { get; init; }
    public string? Tags { get; init; }
    public DateTime? PublishDate { get; init; }

    public bool HasAnyValue =>
        !string.IsNullOrWhiteSpace(Title) ||
        !string.IsNullOrWhiteSpace(Slug) ||
        !string.IsNullOrWhiteSpace(Tags) ||
        PublishDate.HasValue;
}
