using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows;

namespace Minimal_Text_Editor__Lite_.ViewModel.Helpers
{
    public class AddOverlayForModals
    {
        public void AddOverlayToGrid(Grid mainGrid)
        {
            // Criar o overlay com fundo escuro e 50% de opacidade
            var overlay = new Grid
            {
                Background = new SolidColorBrush(Color.FromArgb(120, 0, 0, 0)), // Cor preta semi-transparente
                VerticalAlignment = VerticalAlignment.Stretch,
                HorizontalAlignment = HorizontalAlignment.Stretch,
                Opacity = 0.5
            };

            // Garantir que o overlay esteja acima de tudo
            Panel.SetZIndex(overlay, 99);

            // Adicionar o overlay ao Grid principal
            mainGrid.Children.Add(overlay);
        }

        public void RemoveOverlayFromGrid(Grid mainGrid)
        {
            // Remover o overlay
            var overlayToRemove = mainGrid.Children.OfType<Grid>().FirstOrDefault(child => child.Background is SolidColorBrush brush && brush.Color.A == 120);
            if (overlayToRemove != null)
            {
                mainGrid.Children.Remove(overlayToRemove);
            }
        }
    }
}
