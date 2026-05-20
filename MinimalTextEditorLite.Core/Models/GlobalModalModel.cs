using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace MinimalTextEditorLite.Core.Models
{
    public class GlobalModalModel
    {
        // Propriedades para o primeiro StackPanel
        public string ImageSource { get; set; }      // Fonte da imagem
        public string HeaderContent { get; set; }    // Conteúdo do cabeçalho (Label)

        // Propriedades para o segundo StackPanel
        public bool ShowTextField { get; set; }      // Define se o campo de texto aparece
        public string LabelTextField { get; set; }   // Texto do label relacionado ao campo de texto
        public string TextFieldHint { get; set; }    // Texto de dica do campo de texto
        public bool ShowSimpleText { get; set; }     // Define se o texto simples aparece
        public string SimpleTextContent { get; set; } // Conteúdo do texto simples
        public string BoldTextContent { get; set; }  //Define a parte que ficará em negrito
        public bool ShowConfirmationCheck { get; set; } //Define se o CheckBox foi marcado pelo usuário ou não

        // Propriedades para o terceiro StackPanel (botões)
        public ButtonModel SaveButton { get; set; }  // Modelo para o botão Save
        public ButtonModel CancelButton { get; set; } // Modelo para o botão Cancel
        public ButtonModel OkButton { get; set; }    // Modelo para o botão OK

        public GlobalModalModel()
        {
            // Inicializa os botões com valores padrão, caso não sejam configurados
            SaveButton = new ButtonModel();
            CancelButton = new ButtonModel();
            OkButton = new ButtonModel();
        }
    }
}
