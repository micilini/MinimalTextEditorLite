using Minimal_Text_Editor__Lite_.ViewModel.Helpers;
using SQLite;
using System.ComponentModel;
using System.Configuration;
using System.Data;
using System.IO;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media;

namespace Minimal_Text_Editor__Lite_
{
    /// <summary>
    /// Interaction logic for App.xaml
    /// </summary>
    public partial class App : Application, INotifyPropertyChanged
    {
        //Métodos do INotifyPropertyChanged
        public event PropertyChangedEventHandler PropertyChanged;
        private static Mutex _appMutex;

        protected virtual void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        protected override void OnStartup(StartupEventArgs e)
        {
            // Nome exclusivo do Mutex para evitar múltiplas instâncias
            string appName = @"Local\MinimalTextEditorLite";
            bool createdNew;

            // Cria um Mutex e armazena na variável estática
            _appMutex = new Mutex(true, appName, out createdNew);

            if (!createdNew)
            {
                Environment.Exit(0); // Fecha a nova instância
            }

            // Desabilitar a aceleração de hardware
            RenderOptions.ProcessRenderMode = RenderMode.SoftwareOnly;

            //Cria a pasta "MinimalTextEditorLite" em AppData
            CreateApplicationFolderIfNeeded("MinimalTextEditorLite");

            // Arquivo de Internacionalização
            LocalizationHelper.CreateLocalizationFileIfNedded("en_us");

            // Inicializa a classe de Internacionalização
            Localization = new LocalizationHelper(AppLanguage);

            // Continua com o fluxo normal
            base.OnStartup(e);
        }

        protected override void OnExit(ExitEventArgs e)
        {
            // Libera o Mutex quando a aplicação for fechada
            _appMutex?.ReleaseMutex();
            _appMutex = null;

            base.OnExit(e);
        }

        public void CreateApplicationFolderIfNeeded(string appName)
        {
            // Obtém o caminho da pasta da aplicação em AppData\Local
            string appFolderPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                appName
            );

            // Cria a pasta principal, se necessário
            if (!Directory.Exists(appFolderPath))
            {
                Directory.CreateDirectory(appFolderPath);
            }

            // Cria a subpasta "Backups", se necessário
            string backupsFolderPath = Path.Combine(appFolderPath, "Backups");
            if (!Directory.Exists(backupsFolderPath))
            {
                Directory.CreateDirectory(backupsFolderPath);
            }
        }

        public static LocalizationHelper Localization { get; private set; }
        public string KeyDatabase = string.Empty;
        public string DatabaseFileName = "mte-lite.dll";
        public string ApplicationVersion = "1.0.1";

        public string ApplicationIdentifier { get; set; } 
        private int autoSaveNote;
        public int AutoSaveNote
        {
            get => autoSaveNote;
            set
            {
                if (autoSaveNote != value)
                {
                    autoSaveNote = value;
                    OnPropertyChanged("AutoSaveNote");
                }
            }
        }
        public string AppLanguage { get; set; }
        public bool ShowBackupSizeLimiteMessage { get; set; }
        public bool ShowOpenNoteMessage { get; set; }
        public bool ShowNewUpdates { get; set; }

        private string lastNoteUpdated = string.Empty;
        public string LastNoteUpdated
        {
            get => lastNoteUpdated;
            set
            {
                if (lastNoteUpdated != value)
                {
                    lastNoteUpdated = value;
                    OnPropertyChanged("LastNoteUpdated");
                }
            }
        }
    
        //SQLite (Singleton)
        public SQLiteConnection DBConnection { get; set; }
    }
}
