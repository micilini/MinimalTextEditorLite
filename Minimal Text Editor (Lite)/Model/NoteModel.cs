using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minimal_Text_Editor__Lite_.Model
{
    [Table("Note")]
    public class NoteModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string NoteJson { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public NoteModel()
        {
            NoteJson = "{\"blocks\":[{\"id\":\"XTDk7okQN4\",\"type\":\"header\",\"data\":{\"text\":\"Minimal Text Editor Lite (V 1.0.1)\",\"level\":2}},{\"id\":\"BcnFSGuUCV\",\"type\":\"paragraph\",\"data\":{\"text\":\"Create, edit, and export your notes universally (<i>j</i>son, <i>pdf</i>, <i>doc</i>, and <i>html</i>) with the <b>Minimal Text Editor Lite</b>&nbsp;🥳\"}},{\"id\":\"E_tePyX9Ux\",\"type\":\"paragraph\",\"data\":{\"text\":\"With this open-source minimalist text editor, you will have access to a variety of features that will help you create and format your content efficiently.\"}},{\"id\":\"20cd4gcdg9\",\"type\":\"paragraph\",\"data\":{\"text\":\"Happy Writing 🤓\"}}]}";
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }
    }
}
