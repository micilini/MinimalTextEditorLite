using Microsoft.Extensions.DependencyInjection;
using MinimalTextEditorLite.App.Helpers;
using System.Windows;

namespace MinimalTextEditorLite.App.View.ExportModal;

public partial class ExportModalWindow : Window
{
    private readonly NoteService noteService;

    public ExportModalWindow()
    {
        InitializeComponent();

        noteService = ((App)Application.Current).Services.GetRequiredService<NoteService>();
        ExportComboBox.SelectedIndex = 0;
    }

    private async void ButtonExport_Click(object sender, RoutedEventArgs e)
    {
        ButtonExport.IsEnabled = false;
        ButtonExport.Content = App.Localization.Translate("Export_Wait_Message");

        var exportSucceeded = false;

        try
        {
            switch (ExportComboBox.SelectedIndex)
            {
                case 0:
                    exportSucceeded = await noteService.ExportCurrentNoteAsJSON();
                    break;
                case 1:
                    exportSucceeded = await noteService.ExportCurrentNoteAsDoc();
                    break;
                case 2:
                    exportSucceeded = await noteService.ExportCurrentNoteAsHTML();
                    break;
                case 3:
                    exportSucceeded = await noteService.ExportCurrentNoteAsPDF();
                    break;
                default:
                    ModalMessages.showErrorModal(App.Localization.Translate("Error_Export_1"));
                    break;
            }

            if (exportSucceeded)
            {
                DialogResult = true;
                return;
            }

            ButtonExport.IsEnabled = true;
            ButtonExport.Content = App.Localization.Translate("Export_Button");
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Export_1"));
            ButtonExport.IsEnabled = true;
            ButtonExport.Content = App.Localization.Translate("Export_Button");
        }
    }

    private void ButtonClose_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
