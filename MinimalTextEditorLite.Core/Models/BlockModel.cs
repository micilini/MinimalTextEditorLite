using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalTextEditorLite.Core.Models
{
    public class BlockModel
    {
        public string Id { get; set; }
        public string Type { get; set; }
        public dynamic Data { get; set; }
    }
}
