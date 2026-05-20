using SQLite;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalTextEditorLite.Core.Models
{
    [Table("AppVersion")]
    public class AppVersion
    {
        [PrimaryKey, AutoIncrement]
        public int Id { get; set; }

        public double Version { get; set; }

        public DateTime CreatedAt { get; set; }

        public DateTime UpdatedAt { get; set; }

        public AppVersion()
        {
            Version = 1.0;
            CreatedAt = DateTime.Now;
            UpdatedAt = DateTime.Now;
        }
    }
}
