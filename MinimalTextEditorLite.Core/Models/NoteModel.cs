using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalTextEditorLite.Core.Models
{
    [Table("Note")]
    public class NoteModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string NoteJson { get; set; }

        public string? Title { get; set; }

        public string? Slug { get; set; }

        public string? Tags { get; set; }

        public DateTime? PublishDate { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public NoteModel()
        {
            NoteJson = """
            {
              "time": 0,
              "blocks": [
                {
                  "id": "mte-welcome-title",
                  "type": "header",
                  "data": {
                    "text": "MinimalTextEditor (V 2.0.0)",
                    "level": 2
                  }
                },
                {
                  "id": "mte-welcome-intro",
                  "type": "paragraph",
                  "data": {
                    "text": "Create, edit, import, and export your notes universally (<i>json</i>, <i>markdown</i>, <i>html</i>, <i>pdf</i>, and <i>doc</i>) with <b>MinimalTextEditor</b>. \u2728"
                  }
                },
                {
                  "id": "mte-welcome-features",
                  "type": "paragraph",
                  "data": {
                    "text": "With this open-source minimalist editor, you can write efficiently with structured blocks, images, Markdown, dark mode, Focus Mode, recent files, and clipboard export."
                  }
                },
                {
                  "id": "mte-welcome-happy-writing",
                  "type": "paragraph",
                  "data": {
                    "text": "Happy writing \uD83D\uDE80"
                  }
                }
              ],
              "version": "2.30.6"
            }
            """;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
