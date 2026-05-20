using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinimalTextEditorLite.App.Helpers;
using MinimalTextEditorLite.App.View.SettingsModal;
using MinimalTextEditorLite.Core.Database;
using MinimalTextEditorLite.Core.Models;
using MinimalTextEditorLite.Core.Repositories;
using MinimalTextEditorLite.Core.Services;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Windows;

namespace MinimalTextEditorLite.App.ViewModels;

public partial class SettingsModalWindowVM : ObservableObject
{
    private readonly ISettingsRepository settingsRepository;
    private readonly IBackupService backupService;

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

    public SettingsModalWindowVM(SettingsModalWindow settingsWindow, ISettingsRepository settingsRepository, IBackupService backupService)
    {
        SettingsModalWindow = settingsWindow;
        this.settingsRepository = settingsRepository;
        this.backupService = backupService;

        GetAppConfiguration();
        GetBackupFilesInfo();
    }

    public void GetAppConfiguration()
    {
        var settings = settingsRepository.GetCurrentAsync().GetAwaiter().GetResult();

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
        try
        {
            var stats = backupService.GetStatisticsAsync().GetAwaiter().GetResult();
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
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Backup_Folder_Stats"));
        }
    }

    private static int GetAutoSaveIndexFromValue(int autoSaveValue)
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

    private async void UpdateAutoSaveSetting(AutoSaveInterval interval)
    {
        var settings = await settingsRepository.GetCurrentAsync();

        if (settings != null)
        {
            settings.AutoSaveNote = (int)interval;
            settings.UpdatedAt = DateTime.UtcNow;

            bool isUpdated = await settingsRepository.UpdateAsync(settings);
            ((App)Application.Current).AutoSaveNote = (int)interval;

            if (!isUpdated)
                ModalMessages.showErrorModal(App.Localization.Translate("Error_AutoSave"));
        }
        else
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_AutoSave_Retrive"));
        }
    }

    private async void UpdateLanguageInDatabase(int newLanguageIndex)
    {
        string newLanguage = newLanguageIndex == 0 ? "en_us" : "pt_br";
        var settings = await settingsRepository.GetCurrentAsync();

        if (settings != null)
        {
            settings.Language = newLanguage;
            bool isUpdated = await settingsRepository.UpdateAsync(settings);

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
        if (Directory.Exists(AppPaths.BackupsFolder))
        {
            Process.Start(new ProcessStartInfo
            {
                FileName = AppPaths.BackupsFolder,
                UseShellExecute = true,
            });
        }
    }

    [RelayCommand]
    private async Task DeleteBackup()
    {
        try
        {
            await backupService.RemoveAllAsync();
            SettingsModalWindow.DialogResult = false;
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Backup_Files_Remove"));
        }
    }

    [RelayCommand]
    private async Task RestoreMessages()
    {
        var settings = await settingsRepository.GetCurrentAsync();

        if (settings != null)
        {
            settings.ShowBackupSizeLimiteMessage = true;
            settings.ShowOpenNoteMessage = true;
            settings.ShowNewUpdates = true;
            settings.UpdatedAt = DateTime.UtcNow;

            bool isUpdated = await settingsRepository.UpdateAsync(settings);

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

