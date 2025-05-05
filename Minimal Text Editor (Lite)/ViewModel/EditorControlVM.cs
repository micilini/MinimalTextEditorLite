using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using Minimal_Text_Editor__Lite_.Model;
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
using WindowsInput;

namespace Minimal_Text_Editor__Lite_.ViewModel
{
    public class EditorControlVM : INotifyPropertyChanged, IDisposable
    {
        //Configurações do INotifyPropertyChanged e IDisposable
        public event PropertyChangedEventHandler? PropertyChanged;

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }

        public void Dispose()
        {
            // Limpeza de recursos aqui
        }

        //Propriedades da Classe
        private readonly string virtualHostName = "minimaltexteditorlite.localhost";
        private readonly string folderPath = System.IO.Path.Combine(Directory.GetCurrentDirectory(), "EditorModules", "EditorJS");

        private bool UserCanSaveNote { get; set; } = false;
        private bool IsFirstTimeLoadingNote = true;

        private int progressValue;
        public int ProgressValue
        {
            get => progressValue;
            set
            {
                if (progressValue != value)
                {
                    progressValue = value;
                    OnPropertyChanged("ProgressValue");
                }
            }
        }

        private Visibility webViewVisibility = Visibility.Hidden;
        public Visibility WebViewVisibility
        {
            get => webViewVisibility;
            set
            {
                webViewVisibility = value;
                OnPropertyChanged("WebViewVisibility");
            }
        }

        private Visibility progressBarVisibility = Visibility.Visible;
        public Visibility ProgressBarVisibility
        {
            get => progressBarVisibility;
            set
            {
                progressBarVisibility = value;
                OnPropertyChanged("ProgressBarVisibility");
            }
        }

        MainScreenWindowVM mainScreenWindow { get; set; }
        public WebView2 myWebView { get; set; }

        //Método Construtor
        public EditorControlVM(MainScreenWindowVM mainScreen, WebView2 webView)
        {
            mainScreenWindow = mainScreen;
            myWebView = webView;

            InitializeWebView();
        }

        //Métodos de Controle da WebView
        private async void InitializeWebView()
        {
            // Configurar diretório de dados do WebView2 no AppData do usuário
            string userDataFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
                "MinimalTextEditorLite",
                "WebView2"
            );

            // Certifique-se de que o diretório existe
            Directory.CreateDirectory(userDataFolder);

            // Criar o ambiente do WebView2 com o diretório de dados especificado
            var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
            await myWebView.EnsureCoreWebView2Async(env);

            // Caminho da pasta onde os arquivos HTML estão localizados
            var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "EditorModules", "EditorJS");

            // Verifica se o caminho da pasta é válido
            if (!Directory.Exists(folderPath))
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Editor_Folder"));
                return; // Encerra o método se a pasta não existir
            }

            // Mapeia o nome de host virtual para o caminho da pasta
            myWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
                virtualHostName,
                folderPath,
                CoreWebView2HostResourceAccessKind.DenyCors
            );

            // Carrega a URL usando o nome de host virtual
            string mainUrl = $"https://{virtualHostName}/index.html";

            // Navega para a URL principal
            myWebView.CoreWebView2.Navigate(mainUrl);

            // Configurações para desabilitar contexto, zoom
            myWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
            myWebView.CoreWebView2.Settings.IsWebMessageEnabled = true;
            myWebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
            myWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;

            // Redirecionar para URL principal ao tentar sair dela
            myWebView.CoreWebView2.NavigationStarting += HandleNavigation;

            // Redirecionar as mensagens enviadas pelo Editor para uma função específica
            myWebView.CoreWebView2.WebMessageReceived += myWebView_CoreWebView2_WebMessageReceived;

            // Quando o carregamento for concluído, esconda o indicador de carregamento
            myWebView.CoreWebView2.NavigationCompleted += async (sender, args) =>
            {
                // Inscrever - se para o evento de menu de contexto
                myWebView.CoreWebView2.ContextMenuRequested += MyWebView_CoreWebView2_ContextMenuRequested;

                // Atraso de 1,5 segundos
                await Task.Delay(1200);
                LoadCurrentNote();
                IsFirstTimeLoadingNote = false;
            };
        }

        private void HandleNavigation(object sender, CoreWebView2NavigationStartingEventArgs args)
        {
            // Verificar se a URL não é exatamente a principal
            if (!args.Uri.Equals($"https://{virtualHostName}/index.html", StringComparison.OrdinalIgnoreCase))
            {
                // Cancelar a navegação e redirecionar de volta para a URL principal
                args.Cancel = true;
                myWebView.CoreWebView2.Navigate($"https://{virtualHostName}/index.html");
            }
        }

        public async void saveContent()
        {
            ProgressBarVisibility = Visibility.Visible;
            await Task.Delay(1200);

            // Executa a função handleSave no JavaScript
            myWebView.CoreWebView2.ExecuteScriptAsync("window.handleSave();");
        }

        public void ResetContent()
        {
            // Para limpar o editor
            myWebView.CoreWebView2.ExecuteScriptAsync("window.handleClear();");
        }

        public void InsertContent(string noteJson)
        {
            // Gera o timestamp em milissegundos a partir de C#
            long currentTime = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();

            // Executa o script JavaScript, passando o JSON ajustado (se necessário)
            var escapedJson = noteJson.Replace("\\", "\\\\").Replace("'", "\\'");
            myWebView.CoreWebView2.ExecuteScriptAsync($"window.handleLoad('{escapedJson}');");

            ProgressBarVisibility = Visibility.Collapsed;
            WebViewVisibility = Visibility.Visible;
            UserCanSaveNote = true;
            mainScreenWindow.SaveNoteCompleted = true;
        }

        // Ouça as mensagens do JavaScript
        private void myWebView_CoreWebView2_WebMessageReceived(object sender, CoreWebView2WebMessageReceivedEventArgs e)
        {
            string jsonData = e.TryGetWebMessageAsString();
            UpdateNoteContent(jsonData);
        }

        // Manipulador do evento de menu de contexto
        private void MyWebView_CoreWebView2_ContextMenuRequested(object sender, CoreWebView2ContextMenuRequestedEventArgs args)
        {
            // Acessar a lista de itens do menu de contexto
            IList<CoreWebView2ContextMenuItem> menuList = args.MenuItems;

            // Criar uma lista para armazenar os itens a serem removidos
            List<CoreWebView2ContextMenuItem> itemsToRemove = new List<CoreWebView2ContextMenuItem>();

            // Iterar sobre os itens do menu de contexto
            for (int index = 0; index < menuList.Count; index++)
            {
                // Verificar e adicionar itens à lista de remoção conforme necessário
                if (menuList[index].Name == "back")
                {
                    itemsToRemove.Add(menuList[index]);  // Remover "Voltar"
                }
                else if (menuList[index].Name == "reload")
                {
                    itemsToRemove.Add(menuList[index]);  // Remover "Atualizar"
                }
                else if (menuList[index].Name == "saveAs")
                {
                    itemsToRemove.Add(menuList[index]);  // Remover "Salvar como"
                }
                else if (menuList[index].Name == "saveImageAs")
                {
                    itemsToRemove.Add(menuList[index]);  // Remover "Salvar imagem como"
                }
                else if (menuList[index].Name == "share")
                {
                    itemsToRemove.Add(menuList[index]);  // Remover "Compartilhar"
                }
                else if (menuList[index].Name == "print")
                {
                    itemsToRemove.Add(menuList[index]);  // Remover "Imprimir"
                }
                else if (menuList[index].Name == "forward")
                {
                    itemsToRemove.Add(menuList[index]);  // Remover "Avançar"
                }
                else if (menuList[index].Name == "openLinkInNewWindow")
                {
                    itemsToRemove.Add(menuList[index]);  // Remover "Abrir link em nova janela"
                }
                else if (menuList[index].Name == "webCapture")
                {
                    itemsToRemove.Add(menuList[index]);  // Remover "Captura de tela"
                }
            }

            // Remover os itens da lista de menu
            foreach (var item in itemsToRemove)
            {
                menuList.Remove(item);
            }

            // Não marcar como tratado para permitir que o menu de contexto padrão seja exibido
            // args.Handled = true;  // Não marcamos como 'Handled' para não bloquear a exibição do menu padrão
        }

        //Método de carregamento de Nota Atual
        public async void LoadCurrentNote()
        {
            if (IsFirstTimeLoadingNote == false)
            {
                
                ProgressBarVisibility = Visibility.Visible;
                WebViewVisibility = Visibility.Collapsed;

                await Task.Delay(1200);
            }
            
            var currentNote = DatabaseHelper.Read<NoteModel>().FirstOrDefault();
            
            if (currentNote != null)
            {
                InsertContent(currentNote.NoteJson);
            }
            else
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Notes_Not_Found"));
            }
        }

        private void UpdateNoteContent(string jsonData)
        {
            ProgressBarVisibility = Visibility.Visible;
            try
            {
                NoteService.UpdateCurrentNote(jsonData);
            }
            catch (Exception ex)
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Update_Note"));
            }
            finally
            {
                ProgressBarVisibility = Visibility.Collapsed;
                mainScreenWindow.SaveNoteCompleted = true;
            }
        }

        //Métodos de controle do Header
        public void DoAction(string action)
        {
            switch (action)
            {
                case "Save":
                    Save_Action();
                    break;
                case "Remove":
                    Delete_Action();
                    break;
                case "New":
                    New_Action();
                    break;
                case "Search":
                    OpenSearchBox();
                    break;
                default:
                    break;
            }
        }

        private void Save_Action()
        {
            if (UserCanSaveNote)
                saveContent();
        }

        private void Delete_Action()
        {
            AskIfUserWillDeleteThisNote();
        }

        private void New_Action()
        {
            mainScreenWindow.AddToMainGrid();

            bool result = ModalMessages.ShowConfirmModal(App.Localization.Translate("Confirm_New_Reset_Note_Title"),
                App.Localization.Translate("Confirm_New_Reset_Note_Description"),
                App.Localization.Translate("Confirm_Reset_Note_Bold"));

            if (result)
            {
                RemoveCurrentNote();
            }

            mainScreenWindow.RemoveToMainGrid();
        }

        private void AskIfUserWillDeleteThisNote()
        {

            mainScreenWindow.AddToMainGrid();

            bool result = ModalMessages.ShowConfirmModal(App.Localization.Translate("Confirm_Reset_Note_Title"),
                App.Localization.Translate("Confirm_Reset_Note_Description"),
                App.Localization.Translate("Confirm_Reset_Note_Bold"));

            if (result)
            {
                RemoveCurrentNote();
            }

            mainScreenWindow.RemoveToMainGrid();

        }

        private async void RemoveCurrentNote()
        {
            WebViewVisibility = Visibility.Collapsed;
            ProgressBarVisibility = Visibility.Visible;
            await Task.Delay(1200);

            string query = "UPDATE Note SET NoteJson = ? WHERE Id = ?";

            int rowsAffected = DatabaseHelper.Execute(query, "{\"blocks\": []}", 1);

            if (rowsAffected <= 0)
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_App_Update_Note"));
                return;
            }

            LoadCurrentNote();
        }

        private void OpenSearchBox()
        {
            myWebView.Focus();//This will focus WebView

            //This will simulate CTRL + F:
            var sim = new InputSimulator();
            sim.Keyboard.ModifiedKeyStroke(WindowsInput.Native.VirtualKeyCode.CONTROL, WindowsInput.Native.VirtualKeyCode.VK_F);
        }
    }
}
