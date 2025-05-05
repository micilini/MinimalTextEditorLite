using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using SQLite;

namespace Minimal_Text_Editor__Lite_.Model
{
    [Table("Settings")]
    public class SettingsModel
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public string ApplicationIdentifier { get; set; }

        public int AutoSaveNote { get; set; }

        public string Language { get; set; }

        public bool ShowBackupSizeLimiteMessage { get; set; }

        public bool ShowOpenNoteMessage { get; set; }

        public bool ShowNewUpdates { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public SettingsModel()
        {
            ApplicationIdentifier = GenerateApplicationIdentifier();
            AutoSaveNote = 0;
            Language = "en_us";
            ShowBackupSizeLimiteMessage = true;
            ShowOpenNoteMessage = true;
            ShowNewUpdates = true;
            CreatedAt = DateTime.UtcNow;
            UpdatedAt = DateTime.UtcNow;
        }

        private string GenerateApplicationIdentifier()
        {
            const string chars = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
            var random = new Random();
            return new string(Enumerable.Repeat(chars, 12).Select(s => s[random.Next(s.Length)]).ToArray());
        }
    }
}
