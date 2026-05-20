using SQLite;

namespace MinimalTextEditorLite.Core.Models;

[Table("RecentFile")]
public sealed class RecentFileModel
{
    [PrimaryKey, AutoIncrement]
    public int Id { get; set; }

    [Indexed(Unique = true)]
    public string FilePath { get; set; } = string.Empty;

    public string FileName { get; set; } = string.Empty;

    public DateTime OpenedAt { get; set; } = DateTime.UtcNow;
}
