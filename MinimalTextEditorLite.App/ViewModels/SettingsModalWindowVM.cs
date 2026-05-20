using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinimalTextEditorLite.App.Helpers;
using MinimalTextEditorLite.App.View.SettingsModal;
using MinimalTextEditorLite.Core.Database;
using MinimalTextEditorLite.Core.Models;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace MinimalTextEditorLite.App.ViewModels;

public partial class SettingsModalWindowVM : ObservableObject
{
    private readonly IDatabaseHelper database;
    private readonly NoteService noteService;

    private SettingsModalWindow SettingsModalWindow { get; }

    public bool AllowUserInteract { get; set; }

    [ObservableProperty]
    private string backupFilesText = string.Empty;

    [ObservableProperty]
    private int selectedAutoSaveIndex;

    partial void OnSelectedAutoSaveIndexChanged(int value)
    {
        if (AllowUserInteract)
            UpdateAutoSaveSetting(MapIndexToAutoSaveInterval(value));
    }

    [ObservableProperty]
    private int selectedLanguageIndex;

    partial void OnSelectedLanguageIndexChanged(int value)
    {
        if (AllowUserInteract)
            UpdateLanguageInDatabase(value);
    }

    public SettingsModalWindowVM(SettingsModalWindow settingsWindow, IDatabaseHelper database, NoteService noteService)
    {
        SettingsModalWindow = settingsWindow;
        this.database = database;
        this.noteService = noteService;

        GetAppConfiguration();
        GetBackupFilesInfo();
    }

    public void GetAppConfiguration()
    {
        var settings = database.QuerySingle<SettingsModel>("SELECT * FROM Settings WHERE Id = 1");

        if (settings != null)
        {
            SelectedAutoSaveIndex = GetAutoSaveIndexFromValue(settings.AutoSaveNote);
            SelectedLanguageIndex = settings.Language.Equals("pt_br", StringComparison.OrdinalIgnoreCase) ? 1 : 0;
        }
        else
        {
            SelectedAutoSaveIndex = 0;
            SelectedLanguageIndex = 0;
        }
    }

    public void GetBackupFilesInfo()
    {
        var stats = noteService.GetBackupFolderStatistics();
        var formattedText = new StringBuilder();

        formattedText.Append(App.Localization.Translate("Modal_Settings_Backup_Total_Of"));
        formattedText.Append(" ");
        formattedText.Append(stats.FileCount);
        formattedText.Append(" ");
        formattedText.Append(App.Localization.Translate("Modal_Settings_Backup_Files"));
        formattedText.Append(" ");
        formattedText.Append(stats.TotalSize);
        formattedText.Append(" ");
        formattedText.Append(App.Localization.Translate("Modal_Settings_Backup_Remove_These"));

        BackupFilesText = formattedText.ToString();
    }

    private int GetAutoSaveIndexFromValue(int autoSaveValue)
    {
        return autoSaveValue switch
        {
            0 => 0,
            1 => 1,
            5 => 2,
            15 => 3,
            30 => 4,
            60 => 5,
            120 => 6,
            _ => 0
        };
    }

    private static AutoSaveInterval MapIndexToAutoSaveInterval(int index)
    {
        return index switch
        {
            0 => AutoSaveInterval.Never,
            1 => AutoSaveInterval.OneMinute,
            2 => AutoSaveInterval.FiveMinutes,
            3 => AutoSaveInterval.FifteenMinutes,
            4 => AutoSaveInterval.ThirtyMinutes,
            5 => AutoSaveInterval.OneHour,
            6 => AutoSaveInterval.TwoHours,
            _ => AutoSaveInterval.Never
        };
    }

    private void UpdateAutoSaveSetting(AutoSaveInterval interval)
    {
        var settings = database.QuerySingle<SettingsModel>("SELECT * FROM Settings WHERE Id = 1");

        if (settings != null)
        {
            settings.AutoSaveNote = (int)interval;
            settings.UpdatedAt = DateTime.UtcNow;

            bool isUpdated = database.Update(settings);
            ((App)Application.Current).AutoSaveNote = (int)interval;

            if (!isUpdated)
                ModalMessages.showErrorModal(App.Localization.Translate("Error_AutoSave"));
        }
        else
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_AutoSave_Retrive"));
        }
    }

    private void UpdateLanguageInDatabase(int newLanguageIndex)
    {
        string newLanguage = newLanguageIndex == 0 ? "en_us" : "pt_br";
        var settings = database.QuerySingle<SettingsModel>("SELECT * FROM Settings WHERE Id = 1");

        if (settings != null)
        {
            settings.Language = newLanguage;
            bool isUpdated = database.Update(settings);

            LocalizationHelper.UpdateLocalizationFile(newLanguage);

            if (isUpdated)
                ModalMessages.showInfoModal(App.Localization.Translate("Title_Restart_Application"), App.Localization.Translate("Description_Restart_Application"));
            else
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Language_Update"));
        }
        else
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Language_Update"));
        }
    }

    [RelayCommand]
    private void OpenBackupFolder()
    {
        string backupsFolderPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MinimalTextEditorLite",
            "Backups");

        if (Directory.Exists(backupsFolderPath))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = backupsFolderPath,
                UseShellExecute = true,
            });
        }
    }

    [RelayCommand]
    private void DeleteBackup()
    {
        noteService.RemoveBackupFiles();
        SettingsModalWindow.DialogResult = false;
    }

    [RelayCommand]
    private void RestoreMessages()
    {
        var settings = database.QuerySingle<SettingsModel>("SELECT * FROM Settings WHERE Id = 1");

        if (settings != null)
        {
            settings.ShowBackupSizeLimiteMessage = true;
            settings.ShowOpenNoteMessage = true;
            settings.ShowNewUpdates = true;
            settings.UpdatedAt = DateTime.UtcNow;

            bool isUpdated = database.Update(settings);

            ((App)Application.Current).ShowBackupSizeLimiteMessage = true;
            ((App)Application.Current).ShowOpenNoteMessage = true;
            ((App)Application.Current).ShowNewUpdates = true;

            if (!isUpdated)
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Restore_Messages"));
                return;
            }

            SettingsModalWindow.DialogResult = true;
        }
        else
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Restore_Messages"));
        }
    }

    [RelayCommand]
    private void Cancel()
    {
        SettingsModalWindow.DialogResult = false;
    }
}
