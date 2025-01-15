using Minimal_Text_Editor__Lite_.Model;
using Minimal_Text_Editor__Lite_.ViewModel.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Controls;
using System.Windows.Documents;
using System.Windows;
using Minimal_Text_Editor__Lite_.View.SettingsModal;
using System.Collections.ObjectModel;
using Minimal_Text_Editor__Lite_.ViewModel.Commands;
using System.Diagnostics;
using System.IO;

namespace Minimal_Text_Editor__Lite_.ViewModel
{
    public class SettingsModalWindowVM : INotifyPropertyChanged
    {
        //Métodos do INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //Propriedades da Classe
        SettingsModalWindow SettingsModalWindow { get; set; }
        public bool AllowUserInteract { get; set; } = false;

        public string BackupFilesText { get; set; }

        private int selectedAutoSaveIndex;
        public int SelectedAutoSaveIndex
        {
            get => selectedAutoSaveIndex;
            set
            {
                if (selectedAutoSaveIndex != value)
                {
                    selectedAutoSaveIndex = value;
                    OnPropertyChanged("SelectedAutoSaveIndex");

                    if (AllowUserInteract == true)
                    {
                        UpdateAutoSaveSetting(MapIndexToAutoSaveInterval(value));
                    }
                    
                }
            }
        }

        private int selectedLanguageIndex;
        public int SelectedLanguageIndex
        {
            get => selectedLanguageIndex;
            set
            {
                if (selectedLanguageIndex != value)
                {
                    selectedLanguageIndex = value;
                    OnPropertyChanged("SelectedLanguageIndex");

                    if (AllowUserInteract == true)
                    {
                        UpdateLanguageInDatabase(value);
                    }

                }
            }
        }

        //Comandos da Classe
        public OpenBackupFolderCommand OpenBackupFolderCommand { get; set; }
        public DeleteBackupCommand DeleteBackupCommand { get; set; }
        public CancelCommand<SettingsModalWindowVM> CancelCommand { get; set; }

        //Método Construtor da Classe
        public SettingsModalWindowVM(SettingsModalWindow settingsWindow)
        {
            SettingsModalWindow = settingsWindow;

            OpenBackupFolderCommand = new OpenBackupFolderCommand(this);
            DeleteBackupCommand = new DeleteBackupCommand(this);
            CancelCommand = new CancelCommand<SettingsModalWindowVM>(this);

            GetAppConfiguration();
            GetBackupFilesInfo();
        }

        //Métodos da Classe
        public void GetAppConfiguration()
        {
            var settings = DatabaseHelper.QuerySingle<SettingsModel>("SELECT * FROM Settings WHERE Id = 1");

            if (settings != null)
            {
                SelectedAutoSaveIndex = GetAutoSaveIndexFromValue(settings.AutoSaveNote);

                if (settings.Language.Equals("pt_br", StringComparison.OrdinalIgnoreCase))
                {
                    SelectedLanguageIndex = 1;
                }
                else
                {
                    SelectedLanguageIndex = 0;
                }
            }
            else
            {
                SelectedAutoSaveIndex = 0;
                SelectedLanguageIndex = 0;
            }
        }

        public void GetBackupFilesInfo()
        {
            var stats = NoteService.GetBackupFolderStatistics();
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

            // Formatação para exibir a contagem e tamanho em negrito
            BackupFilesText = $"{App.Localization.Translate("Modal_Settings_Backup_Total_Of")} {stats.FileCount} {App.Localization.Translate("Modal_Settings_Backup_Files")}{stats.TotalSize}{App.Localization.Translate("Modal_Settings_Backup_Remove_These")}";
        }

        private int GetAutoSaveIndexFromValue(int autoSaveValue)
        {
            switch (autoSaveValue)
            {
                case 0:
                    return 0;
                case 1:
                    return 1;
                case 5:
                    return 2;
                case 15:
                    return 3;
                case 30:
                    return 4;
                case 60:
                    return 5;
                case 120:
                    return 6;
                default:
                    return 0;
            }
        }

        public void CancelButton()
        {
            SettingsModalWindow.DialogResult = false;
        }

        private AutoSaveInterval MapIndexToAutoSaveInterval(int index)
        {
            switch (index)
            {
                case 0:
                    return AutoSaveInterval.Never;
                case 1:
                    return AutoSaveInterval.OneMinute;
                case 2:
                    return AutoSaveInterval.FiveMinutes;
                case 3:
                    return AutoSaveInterval.FifteenMinutes;
                case 4:
                    return AutoSaveInterval.ThirtyMinutes;
                case 5:
                    return AutoSaveInterval.OneHour;
                case 6:
                    return AutoSaveInterval.TwoHours;
                default:
                    return AutoSaveInterval.Never;
            }
        }

        private void UpdateAutoSaveSetting(AutoSaveInterval interval)
        {
            var settings = DatabaseHelper.QuerySingle<SettingsModel>("SELECT * FROM Settings WHERE Id = 1");

            if (settings != null)
            {
                settings.AutoSaveNote = (int)interval;

                settings.UpdatedAt = DateTime.UtcNow;

                bool isUpdated = DatabaseHelper.Update(settings);

                ((App)Application.Current).AutoSaveNote = (int)interval;

                if (!isUpdated)
                {
                    // Caso falhe ao atualizar, mostra uma mensagem de erro
                    ModalMessages.showErrorModal(App.Localization.Translate("Error_AutoSave"));
                }
            }
            else
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_AutoSave_Retrive"));
            }
        }

        private void UpdateLanguageInDatabase(int newLanguageIndex)
        {
            string newLanguage = (newLanguageIndex == 0) ? "en_us" : "pt_br";

            var settings = DatabaseHelper.QuerySingle<SettingsModel>("SELECT * FROM Settings WHERE Id = 1");

            if (settings != null)
            {
                settings.Language = newLanguage;

                bool isUpdated = DatabaseHelper.Update(settings);

                LocalizationHelper.UpdateLocalizationFile(newLanguage);

                if (isUpdated)
                {
                    ModalMessages.showInfoModal(App.Localization.Translate("Title_Restart_Application"), App.Localization.Translate("Description_Restart_Application"));
                }
                else
                {
                    ModalMessages.showErrorModal(App.Localization.Translate("Error_Language_Update"));
                }
            }
            else
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Language_Update"));
            }
        }

        public void OpenBackupFolder()
        {
            string backupsFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MinimalTextEditorLite",
                "Backups"
            );

            if (Directory.Exists(backupsFolderPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = backupsFolderPath,
                    UseShellExecute = true,
                });
            }
        }

        public void DeleteBackupFile()
        {
            NoteService.RemoveBackupFiles();
            SettingsModalWindow.DialogResult = false;
        }
    }
}
