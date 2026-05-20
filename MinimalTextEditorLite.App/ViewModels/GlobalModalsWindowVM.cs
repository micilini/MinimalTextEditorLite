using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinimalTextEditorLite.App.Helpers;
using MinimalTextEditorLite.App.View.GlobalModals;
using MinimalTextEditorLite.Core.Database;
using MinimalTextEditorLite.Core.Models;
using System.Windows;

namespace MinimalTextEditorLite.App.ViewModels;

public partial class GlobalModalsWindowVM : ObservableObject
{
    private readonly IDatabaseHelper database;

    [ObservableProperty]
    private GlobalModalsWindow globalModalsWindow;

    [ObservableProperty]
    private GlobalModalModel globalModalModel;

    [ObservableProperty]
    private string inputResult = string.Empty;

    [ObservableProperty]
    private bool isInputModal;

    public GlobalModalsWindowVM(GlobalModalsWindow globalModals, GlobalModalModel gmm, bool inputModal, IDatabaseHelper database)
    {
        this.database = database;
        globalModalsWindow = globalModals;
        globalModalModel = gmm;
        isInputModal = inputModal;
    }

    [RelayCommand]
    private void Next()
    {
        GlobalModalsWindow.DialogResult = true;
    }

    [RelayCommand]
    private void Cancel()
    {
        GlobalModalsWindow.DialogResult = false;
    }

    [RelayCommand]
    private void Ok()
    {
        GlobalModalsWindow.DialogResult = false;
    }

    public void UpdateShowOpenNoteMessage(bool value)
    {
        try
        {
            var settings = database.QuerySingle<SettingsModel>("SELECT * FROM Settings WHERE Id = ?", 1);

            if (settings != null)
            {
                settings.ShowOpenNoteMessage = value;
                settings.UpdatedAt = DateTime.UtcNow;
                database.Update(settings);
                ((App)Application.Current).ShowOpenNoteMessage = value;
            }
            else
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Update_Note"));
            }
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Update_Note"));
        }
    }

    public void UpdateShowBackupSizeMessage(bool value)
    {
        try
        {
            var settings = database.QuerySingle<SettingsModel>("SELECT * FROM Settings WHERE Id = ?", 1);

            if (settings != null)
            {
                settings.ShowBackupSizeLimiteMessage = value;
                settings.UpdatedAt = DateTime.UtcNow;
                database.Update(settings);
                ((App)Application.Current).ShowBackupSizeLimiteMessage = value;
            }
            else
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Update_BackupSize"));
            }
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Update_BackupSize"));
        }
    }
}
