using Minimal_Text_Editor__Lite_.Model;
using SQLite;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Minimal_Text_Editor__Lite_.ViewModel.Helpers
{
    public class StartupAppConfiguration
    {
        private string _keyFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MinimalTextEditorLite",
            "dt-app.mte"
        );

        private string _dbFilePath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MinimalTextEditorLite",
            ((App)Application.Current).DatabaseFileName
        );

        public bool CheckAndCreateDatabase()
        {
            if (!File.Exists(_keyFilePath))
            {
                var timestamp = DateTimeOffset.UtcNow.ToUnixTimeSeconds().ToString();
                File.WriteAllText(_keyFilePath, timestamp);
            }

            if (!File.Exists(_dbFilePath))
            {
                ((App)Application.Current).KeyDatabase = GetEncryptionKey();
                StartDBSingleton();
                CreateDatabaseAndTables();
                GetAppConfigurationSettings();
                return false;
            }

            ((App)Application.Current).KeyDatabase = GetEncryptionKey();
            StartDBSingleton();
            GetAppConfigurationSettings();

            return true;
        }

        private void CreateDatabaseAndTables()
        {
            var encryptionKey = GetEncryptionKey();
            var connectionString = new SQLiteConnectionString(_dbFilePath, true, encryptionKey);

            using (var connection = new SQLiteConnection(connectionString))
            {
                connection.CreateTable<AppVersion>();
                InsertAppVersion(connection);

                connection.CreateTable<SettingsModel>();
                InsertDefaultSystemSettings(connection);

                connection.CreateTable<NoteModel>();
                InsertNewNote(connection);
            }
        }

        private void InsertAppVersion(SQLiteConnection connection)
        {
            var newVersion = new AppVersion();
            connection.Insert(newVersion);
        }

        private void InsertDefaultSystemSettings(SQLiteConnection connection)
        {
            var newSettings = new SettingsModel();
            connection.Insert(newSettings);
        }

        private void InsertNewNote(SQLiteConnection connection)
        {
            var newNote = new NoteModel();
            connection.Insert(newNote);
        }

        private void GetAppConfigurationSettings()
        {
            try
            {
                // Query para buscar a configuração com Id = 1
                var query = "SELECT * FROM Settings WHERE Id = ?";
                var settings = DatabaseHelper.QuerySingle<SettingsModel>(query, 1);

                if (settings != null)
                {
                    ((App)Application.Current).ApplicationIdentifier = settings.ApplicationIdentifier;
                    ((App)Application.Current).AutoSaveNote = settings.AutoSaveNote;
                    ((App)Application.Current).AppLanguage = settings.Language;
                    ((App)Application.Current).ShowBackupSizeLimiteMessage = settings.ShowBackupSizeLimiteMessage;
                    ((App)Application.Current).ShowOpenNoteMessage = settings.ShowOpenNoteMessage;
                    ((App)Application.Current).ShowNewUpdates = settings.ShowNewUpdates;
                }
                else
                {
                    Application.Current.Dispatcher.Invoke(() =>
                    {
                        ModalMessages.showErrorModal(App.Localization.Translate("Error_App_Settings_Not_Found"));
                        Application.Current.Shutdown();
                    });
                }
            }
            catch (Exception ex)
            {
                Application.Current.Dispatcher.Invoke(() =>
                {
                    ModalMessages.showErrorModal(App.Localization.Translate("Error_App_Settings_Not_Found"));
                    Application.Current.Shutdown();
                });
            }
        }

        private string GetEncryptionKey()
        {
            if (File.Exists(_keyFilePath))
            {
                return File.ReadAllText(_keyFilePath);
            }

            Application.Current.Dispatcher.Invoke(() =>
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_App_Settings_Not_Found"));
                Application.Current.Shutdown();
            });

            return string.Empty;
        }

        private void StartDBSingleton()
        {
            ((App)Application.Current).DBConnection = DatabaseConnectionManager.GetConnection();
        }
    }
}
