using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalTextEditorLite.Core.Models
{
    public class ButtonModel
    {
        public string Text { get; set; }      // Texto do botão
        public bool IsVisible { get; set; }   // Visibilidade do botão

        public ButtonModel()
        {
            IsVisible = true; // Valor padrão
        }
    }
}
