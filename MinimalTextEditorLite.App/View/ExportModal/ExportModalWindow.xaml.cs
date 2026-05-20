using Microsoft.Extensions.DependencyInjection;
using Microsoft.Win32;
using MinimalTextEditorLite.App.Helpers;
using MinimalTextEditorLite.Core.Services;
using System.IO;
using System.Windows;

namespace MinimalTextEditorLite.App.View.ExportModal;

public partial class ExportModalWindow : Window
{
    private readonly IExportService exportService;

    public ExportModalWindow()
    {
        InitializeComponent();

        exportService = ((App)Application.Current).Services.GetRequiredService<IExportService>();
        ExportComboBox.SelectedIndex = 0;
        MarkdownImageModeComboBox.SelectedIndex = 0;
    }

    private async void ButtonExport_Click(object sender, RoutedEventArgs e)
    {
        ButtonExport.IsEnabled = false;
        ButtonExport.Content = App.Localization.Translate("Export_Wait_Message");

        try
        {
            var exporterId = ExportComboBox.SelectedIndex switch
            {
                0 => "json",
                1 => "doc",
                2 => "html",
                3 => "md",
                4 => "pdf",
                _ => string.Empty
            };

            if (string.IsNullOrEmpty(exporterId))
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Export_1"));
                ResetExportButton();
                return;
            }

            if (ShouldExportMarkdownAssetsToFolder())
            {
                var folderDialog = new OpenFolderDialog
                {
                    Title = App.Localization.Translate("Title_Export_Note_Select_Folder")
                };

                if (folderDialog.ShowDialog() != true)
                {
                    ResetExportButton();
                    return;
                }

                await exportService.ExportMarkdownWithAssetsAsync(folderDialog.FolderName);
                DialogResult = true;
                return;
            }

            var descriptor = exportService.GetExporter(exporterId);
            var saveFileDialog = new SaveFileDialog
            {
                Filter = descriptor.FileDialogFilter,
                FileName = descriptor.DefaultFileName,
                Title = App.Localization.Translate("Title_Export_Note")
            };

            if (saveFileDialog.ShowDialog() != true)
            {
                ResetExportButton();
                return;
            }

            var bytes = await exportService.ExportAsync(exporterId);
            await File.WriteAllBytesAsync(saveFileDialog.FileName, bytes);

            DialogResult = true;
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Export_1"));
            ResetExportButton();
        }
    }

    private bool IsMarkdownSelected() => ExportComboBox.SelectedIndex == 3;

    private bool ShouldExportMarkdownAssetsToFolder()
    {
        return IsMarkdownSelected() && MarkdownImageModeComboBox.SelectedIndex == 1;
    }

    private void ExportComboBox_SelectionChanged(object sender, System.Windows.Controls.SelectionChangedEventArgs e)
    {
        if (MarkdownImageOptionsPanel == null)
            return;

        MarkdownImageOptionsPanel.Visibility = IsMarkdownSelected()
            ? Visibility.Visible
            : Visibility.Collapsed;
    }

    private void ResetExportButton()
    {
        ButtonExport.IsEnabled = true;
        ButtonExport.Content = App.Localization.Translate("Export_Button");
    }

    private void ButtonClose_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
