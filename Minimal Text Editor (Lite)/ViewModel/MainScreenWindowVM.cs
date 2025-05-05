using DocumentFormat.OpenXml.Wordprocessing;
using Minimal_Text_Editor__Lite_.View;
using Minimal_Text_Editor__Lite_.View.AboutModal;
using Minimal_Text_Editor__Lite_.View.Components;
using Minimal_Text_Editor__Lite_.View.ExportModal;
using Minimal_Text_Editor__Lite_.View.SettingsModal;
using Minimal_Text_Editor__Lite_.ViewModel.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace Minimal_Text_Editor__Lite_.ViewModel
{
    public class MainScreenWindowVM : INotifyPropertyChanged
    {
        //Configurações Essências da Classe de ViewModel
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        //Propriedades da Classe
        public MainScreenWindow MainScreenWindow { get; set; }

        public bool SaveNoteCompleted { get; set; }

        private BackgroundService _backgroundService;

        EditorControl editorControl { get; set; }

        AddOverlayForModals AddOverlayForModals { get; set; }

        private object headerContent;
        public object HeaderContent
        {
            get => headerContent;
            set
            {
                headerContent = value;
                OnPropertyChanged("HeaderContent");
            }
        }
        
        private object mainContent;
        public object MainContent
        {
            get => mainContent;
            set
            {
                mainContent = value;
                OnPropertyChanged("MainContent");
            }
        }

        private string lastSaveTextBlock;
        public string LastSaveTextBlock
        {
            get => lastSaveTextBlock;
            set
            {
                if (lastSaveTextBlock != value)
                {
                    lastSaveTextBlock = value;
                    OnPropertyChanged("LastSaveTextBlock");
                }
            }
        }

        AppMenuControl appMenuControl { get; set; }

        private object menuContent;
        public object MenuContent
        {
            get => menuContent;
            set
            {
                menuContent = value;
                OnPropertyChanged("MenuContent");
            }
        }

        //Construtor da Classe
        public MainScreenWindowVM(MainScreenWindow mainScreen)
        {
            MainScreenWindow = mainScreen;
            SaveNoteCompleted = true;

            editorControl = new EditorControl(this);
            MenuContent = new AppMenuControl(this);
            HeaderContent = new HeaderControl(this);
            MainContent = editorControl;

            // Subscreve o evento PropertyChanged no App
            var app = (App)Application.Current;
            app.PropertyChanged += App_PropertyChanged;

            // Inicializa com o valor atual
            LastSaveTextBlock = app.LastNoteUpdated;

            //Métodos de Loaded e UnLoaded
            MainScreenWindow.Loaded += MainScreenWindow_Loaded;
            MainScreenWindow.Unloaded += MainScreenWindow_Unloaded;

            //Busca por Atualizações:
            SearchForUpdates();
        }

        //Métodos de Overlay para Modais
        public void AddToMainGrid()
        {
            editorControl.Visibility = Visibility.Collapsed;

            if (MainScreenWindow.Content is Grid mainGrid)
            {
                // Criar uma instância do helper
                AddOverlayForModals = new AddOverlayForModals();

                // Adicionar o overlay ao Grid principal
                AddOverlayForModals.AddOverlayToGrid(mainGrid);
            }
        }

        public void RemoveToMainGrid()
        {
            if (MainScreenWindow.Content is Grid mainGrid)
            {
                // Quando o modal for fechado, remover o overlay
                AddOverlayForModals.RemoveOverlayFromGrid(mainGrid);
            }

            editorControl.Visibility = Visibility.Visible;
        }

        //Método PropertyChanged
        private void App_PropertyChanged(object sender, System.ComponentModel.PropertyChangedEventArgs e)
        {
            if (e.PropertyName == nameof(App.AutoSaveNote))
            {
                int newInterval = ((App)Application.Current).AutoSaveNote;

                if (newInterval == 0)
                {
                    // Intervalo 0: Desativa o serviço
                    _backgroundService?.Stop();
                    _backgroundService = null; // Libera a referência para evitar reutilização
                }
                else
                {
                    if (_backgroundService == null)
                    {
                        // Cria um novo serviço caso não exista (após um intervalo 0)
                        _backgroundService = new BackgroundService(SaveNote, newInterval);
                        _backgroundService.Start();
                    }
                    else
                    {
                        // Atualiza o intervalo se o serviço já estiver ativo
                        _backgroundService.UpdateInterval(newInterval);
                    }
                }
            }
            if (e.PropertyName == "LastNoteUpdated")
            {
                var app = (App)sender;
                LastSaveTextBlock = app.LastNoteUpdated;
            }
        }

        //Load e UnLoad da Window:
        private void MainScreenWindow_Loaded(object sender, RoutedEventArgs e)
        {
            int autoSaveInterval = ((App)Application.Current).AutoSaveNote;

            if (autoSaveInterval > 0)
            {
                _backgroundService = new BackgroundService(SaveNote, autoSaveInterval);
                _backgroundService.Start();
            }

            // Monitor for changes in AutoSaveNote.
            ((App)Application.Current).PropertyChanged += App_PropertyChanged;

            // Check if Backup Folder has more than 600MB
            CheckIfBackupFolderHasReachedSizeLimit();
        }

        private void MainScreenWindow_Unloaded(object sender, RoutedEventArgs e)
        {
            _backgroundService?.Dispose();
        }

        //Método para buscar novas atualizações
        private void SearchForUpdates()
        {
            UpdatesCheck updatesCheck = new UpdatesCheck();
            updatesCheck.CheckForUpdates();
        }

        // Método para verificar se a pasta de backup possui mais que 600MB
        private async void CheckIfBackupFolderHasReachedSizeLimit()
        {
            // 1) Espera 3 segundos (sem travar a interface)
            await Task.Delay(TimeSpan.FromSeconds(3));

            if (((App)Application.Current).ShowBackupSizeLimiteMessage)
            {
                string localAppData = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
                string baseFolder = Path.Combine(localAppData, "MinimalTextEditorLite");
                string backupsFolder = Path.Combine(baseFolder, "Backups");

                if (Directory.Exists(backupsFolder))
                {
                    long folderSize = GetDirectorySize(backupsFolder);
                    const long sizeLimit = 600L * 1024 * 1024; // 600 MB

                    if (folderSize > sizeLimit)
                    {
                        AddToMainGrid();

                        bool result = ModalMessages.ShowBackupSizeMessageConfim(
                            App.Localization.Translate("Title_Backup_Limit"),
                            App.Localization.Translate("Description_Backup_Limit"),
                            ""
                        );

                        RemoveToMainGrid();
                    }
                }
            }
        }

        private long GetDirectorySize(string folderPath)
        {
            long totalSize = 0;

            try
            {
                foreach (string file in Directory.GetFiles(folderPath, "*", SearchOption.AllDirectories))
                {
                    try
                    {
                        var info = new FileInfo(file);
                        totalSize += info.Length;
                    }
                    catch
                    {
                        // Ignora arquivos inacessíveis
                    }
                }
            }
            catch
            {
                // Problema ao acessar a pasta — opcionalmente logue ou trate
            }

            return totalSize;
        }

        //Métodos de Interação Entre UserControls
        public void OpenNewNote()
        {
            if (((App)Application.Current).ShowOpenNoteMessage)
            {
                AddToMainGrid();

                bool result = ModalMessages.ShowConfirmOpenNoteModal(App.Localization.Translate("Global_Modal_Open_Note_Title"),
                    App.Localization.Translate("Global_Modal_Open_Note_Description"),
                    App.Localization.Translate("Global_Modal_Open_Note_Bold"));

                RemoveToMainGrid();

                if (!result)
                {
                    return;
                }
            }

            if (NoteService.OpenNewNote())
            {
                editorControl.LoadCurrentNote();
            }
        }

        public void SaveNote()
        {

            if (!SaveNoteCompleted)
            {
                return;
            }

            SaveNoteCompleted = false;

            // Verifica se o código está na thread principal
            if (Application.Current.Dispatcher.CheckAccess())
            {
               // Se já estiver na thread principal, chama o DoAction diretamente
               editorControl.DoAction("Save");
            }
            else
            {
               // Se não estiver na thread principal, usa o Dispatcher para chamar na thread principal
               Application.Current.Dispatcher.Invoke(() => editorControl.DoAction("Save"));
            }
        }

        public void RemoveNote()
        {
            editorControl.DoAction("Remove");
        }

        public void NewNote()
        {
            editorControl.DoAction("New");
        }

        public void OpenSettingsDialog()
        {
            AddToMainGrid();

            // Criar a janela modal (SettingsModalWindow)
            SettingsModalWindow settingsModal = new SettingsModalWindow();

            // Exibir a janela modal
            bool? dialogResult = settingsModal.ShowDialog();

            RemoveToMainGrid();
        }
        
        public void ExportNoteDialog()
        {
            //Save Note Before Open Exportation Modal
            SaveNote();

            //After Save Note, Open Modal
            AddToMainGrid();

            ExportModalWindow exportModal = new ExportModalWindow();
            bool? dialogResult = exportModal.ShowDialog();

            RemoveToMainGrid();

            if ((bool)dialogResult)
            {
                AddToMainGrid();
                ModalMessages.ShowSuccessModal(App.Localization.Translate("Export_Note_Success_Title"), App.Localization.Translate("Export_Note_Success_Message"));
                RemoveToMainGrid();
            }
        }

        public void SearchNote()
        {
            editorControl.DoAction("Search");
        }

        public void OpenAboutApp()
        {
            AddToMainGrid();

            AboutModalWindow aboutModal = new AboutModalWindow();
            aboutModal.ShowDialog();

            RemoveToMainGrid();
        }

        // Teclas de Atalhos do Sistema
        public void OnPreviewKeyDown(KeyEventArgs e)
        {
            if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
            {
                MenuClick("miNew");
                e.Handled = true;
                return;
            }
            if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
            {
                MenuClick("miOpen");
                e.Handled = true;
                return;
            }
            if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
            {
                MenuClick("miSave");
                e.Handled = true;
                return;
            }
            if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.Control)
            {
                MenuClick("miDelete");
                e.Handled = true;
                return;
            }
            if (e.Key == Key.E && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
            {
                MenuClick("miExport");
                e.Handled = true;
                return;
            }
            if (e.Key == Key.OemComma && Keyboard.Modifiers == ModifierKeys.Control)
            {
                MenuClick("miConfiguration");
                e.Handled = true;
                return;
            }
        }

        // Métodos para cliques no Menu Superior
        public void MenuClick(string action)
        {
            switch (action)
            {
                case "miNew":
                    NewNote();
                    break;
                case "miOpen":
                    OpenNewNote();
                    break;
                case "miSave":
                    SaveNote();
                    break;
                case "miDelete":
                    RemoveNote();
                    break;
                case "miExport":
                    ExportNoteDialog();
                    break;
                case "miConfiguration":
                    OpenSettingsDialog();
                    break;
                case "miExit":
                    Application.Current.Shutdown();
                    break;
                case "miAbout":
                    OpenAboutApp();
                    break;
            }
        }
    }
}