using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinimalTextEditorLite.App.Helpers;
using MinimalTextEditorLite.App.Services;
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
    private readonly IThemeService themeService;

    private SettingsModalWindow SettingsModalWindow { get; }

    public bool AllowUserInteract { get; set; }

    [ObservableProperty]
    private string backupFilesText = string.Empty;

    [ObservableProperty]
    private int selectedAutoSaveIndex;

    partial void OnSelectedAutoSaveIndexChanged(int value)
    {
        if (AllowUserInteract)
            _ = UpdateAutoSaveSettingSafeAsync(MapIndexToAutoSaveInterval(value));
    }

    [ObservableProperty]
    private int selectedLanguageIndex;

    partial void OnSelectedLanguageIndexChanged(int value)
    {
        if (AllowUserInteract)
            _ = UpdateLanguageInDatabaseSafeAsync(value);
    }

    [ObservableProperty]
    private int selectedThemeIndex;

    partial void OnSelectedThemeIndexChanged(int value)
    {
        if (AllowUserInteract)
            _ = UpdateThemeSafeAsync(MapIndexToThemePreference(value));
    }

    [ObservableProperty]
    private bool exportFrontMatterYaml;

    partial void OnExportFrontMatterYamlChanged(bool value)
    {
        if (AllowUserInteract)
            _ = UpdateExportFrontMatterYamlSafeAsync(value);
    }

    [ObservableProperty]
    private bool associateFilesWithApp;

    partial void OnAssociateFilesWithAppChanged(bool value)
    {
        if (AllowUserInteract)
            _ = UpdateAssociateFilesSafeAsync(value);
    }

    public SettingsModalWindowVM(
        SettingsModalWindow settingsWindow,
        ISettingsRepository settingsRepository,
        IBackupService backupService,
        IThemeService themeService)
    {
        SettingsModalWindow = settingsWindow;
        this.settingsRepository = settingsRepository;
        this.backupService = backupService;
        this.themeService = themeService;

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
            SelectedThemeIndex = GetThemeIndexFromPreference(settings.Theme);
            ExportFrontMatterYaml = settings.ExportFrontMatterYaml;
            AssociateFilesWithApp = settings.AssociateFilesWithApp || ShellExtensionInstaller.IsInstalled();
        }
        else
        {
            SelectedAutoSaveIndex = 0;
            SelectedLanguageIndex = 0;
            SelectedThemeIndex = 0;
            ExportFrontMatterYaml = true;
            AssociateFilesWithApp = ShellExtensionInstaller.IsInstalled();
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

    private static int GetThemeIndexFromPreference(string? preference)
    {
        var normalized = AppThemePreference.Normalize(preference);

        return normalized switch
        {
            AppThemePreference.Dark => 1,
            AppThemePreference.System => 2,
            _ => 0
        };
    }

    private static string MapIndexToThemePreference(int index)
    {
        return index switch
        {
            1 => AppThemePreference.Dark,
            2 => AppThemePreference.System,
            _ => AppThemePreference.Light
        };
    }

    private async Task UpdateAutoSaveSettingSafeAsync(AutoSaveInterval interval)
    {
        try
        {
            await UpdateAutoSaveSettingAsync(interval);
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_AutoSave"));
        }
    }

    private async Task UpdateAutoSaveSettingAsync(AutoSaveInterval interval)
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

    private async Task UpdateLanguageInDatabaseSafeAsync(int newLanguageIndex)
    {
        try
        {
            await UpdateLanguageInDatabaseAsync(newLanguageIndex);
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Language_Update"));
        }
    }

    private async Task UpdateLanguageInDatabaseAsync(int newLanguageIndex)
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

    private async Task UpdateThemeSafeAsync(string themePreference)
    {
        try
        {
            await UpdateThemeAsync(themePreference);
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Settings_Update"));
        }
    }

    private async Task UpdateThemeAsync(string themePreference)
    {
        var settings = await settingsRepository.GetCurrentAsync();

        if (settings == null)
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_App_Settings_Not_Found"));
            return;
        }

        settings.Theme = AppThemePreference.Normalize(themePreference);
        settings.UpdatedAt = DateTime.UtcNow;

        if (!await settingsRepository.UpdateAsync(settings))
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Settings_Update"));
            return;
        }

        themeService.Apply(settings.Theme);
    }

    private async Task UpdateExportFrontMatterYamlSafeAsync(bool value)
    {
        try
        {
            await UpdateExportFrontMatterYamlAsync(value);
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Settings_Update"));
        }
    }

    private async Task UpdateExportFrontMatterYamlAsync(bool value)
    {
        var settings = await settingsRepository.GetCurrentAsync();

        if (settings == null)
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_App_Settings_Not_Found"));
            return;
        }

        settings.ExportFrontMatterYaml = value;
        settings.UpdatedAt = DateTime.UtcNow;

        if (!await settingsRepository.UpdateAsync(settings))
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Settings_Update"));
    }

    private async Task UpdateAssociateFilesSafeAsync(bool value)
    {
        try
        {
            if (value)
                ShellExtensionInstaller.Install();
            else
                ShellExtensionInstaller.Uninstall();

            var settings = await settingsRepository.GetCurrentAsync();

            if (settings == null)
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_App_Settings_Not_Found"));
                return;
            }

            settings.AssociateFilesWithApp = value;
            settings.UpdatedAt = DateTime.UtcNow;

            if (!await settingsRepository.UpdateAsync(settings))
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Settings_Update"));
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Shell_Association_Update"));
            AssociateFilesWithApp = ShellExtensionInstaller.IsInstalled();
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
        var confirmed = ModalMessages.ShowConfirmModal(
            App.Localization.Translate("Confirm_Delete_Backups_Title"),
            App.Localization.Translate("Confirm_Delete_Backups_Description"),
            App.Localization.Translate("Confirm_Delete_Backups_Bold"));

        if (!confirmed)
            return;

        try
        {
            await backupService.RemoveAllAsync();
            GetBackupFilesInfo();
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
            settings.UpdatedAt = DateTime.UtcNow;

            bool isUpdated = await settingsRepository.UpdateAsync(settings);

            ((App)Application.Current).ShowBackupSizeLimiteMessage = true;
            ((App)Application.Current).ShowOpenNoteMessage = true;

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

