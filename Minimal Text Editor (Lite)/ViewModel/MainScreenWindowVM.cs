using Minimal_Text_Editor__Lite_.View;
using Minimal_Text_Editor__Lite_.View.Components;
using Minimal_Text_Editor__Lite_.View.SettingsModal;
using Minimal_Text_Editor__Lite_.ViewModel.Helpers;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;

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

        //Construtor da Classe
        public MainScreenWindowVM(MainScreenWindow mainScreen)
        {
            MainScreenWindow = mainScreen;
            SaveNoteCompleted = true;

            editorControl = new EditorControl(this);

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

            NoteService.OpenNewNote();
            editorControl.LoadCurrentNote();
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

        public void OpenSettingsDialog()
        {
            AddToMainGrid();

            // Criar a janela modal (SettingsModalWindow)
            SettingsModalWindow settingsModal = new SettingsModalWindow();

            // Exibir a janela modal
            bool? dialogResult = settingsModal.ShowDialog();

            RemoveToMainGrid();
        }

        public void SearchNote()
        {
            editorControl.DoAction("Search");
        }

    }
}
