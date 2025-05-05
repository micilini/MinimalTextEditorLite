using Minimal_Text_Editor__Lite_.ViewModel.Helpers;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;

namespace Minimal_Text_Editor__Lite_.View.ExportModal
{
    /// <summary>
    /// Interaction logic for ExportModalWindow.xaml
    /// </summary>
    public partial class ExportModalWindow : Window
    {
        public ExportModalWindow()
        {
            InitializeComponent();
            ExportComboBox.SelectedIndex = 0;
        }

        private async void ButtonExport_Click(object sender, RoutedEventArgs e)
        {
            ButtonExport.IsEnabled = false;
            ButtonExport.Content = App.Localization.Translate("Export_Wait_Message");
            bool exportSucceeded = false;

            try
            {
                switch (ExportComboBox.SelectedIndex)
                {
                    case 0:
                        exportSucceeded = await NoteService.ExportCurrentNoteAsJSON();
                        break;
                    case 1:
                        exportSucceeded = await NoteService.ExportCurrentNoteAsDoc();
                        break;
                    case 2:
                        exportSucceeded = await NoteService.ExportCurrentNoteAsHTML();
                        break;
                    case 3:
                        exportSucceeded = await NoteService.ExportCurrentNoteAsPDF();
                        break;
                    default:
                        ModalMessages.showErrorModal($"{App.Localization.Translate("Error_Export_1")}");
                        break;
                }

                if (exportSucceeded)
                {
                    this.DialogResult = true;
                }
                else
                {
                    ButtonExport.IsEnabled = true;
                    ButtonExport.Content = App.Localization.Translate("Export_Button");
                }

            }
            catch (Exception ex)
            {
                ModalMessages.showErrorModal($"{App.Localization.Translate("Error_Export_1")}");
                ButtonExport.IsEnabled = true;
                ButtonExport.Content = App.Localization.Translate("Export_Button");
            }
        }

        private void ButtonClose_Click(object sender, RoutedEventArgs e)
        {
            this.DialogResult = false;
        }
    }
}
