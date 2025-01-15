using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Minimal_Text_Editor__Lite_.Model
{
    public class NoteJsonModel
    {
        public long Time { get; set; }
        public List<BlockModel> Blocks { get; set; }
        public string Version { get; set; }
    }
}
