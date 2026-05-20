using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinimalTextEditorLite.App.Helpers;
using MinimalTextEditorLite.App.View;
using MinimalTextEditorLite.App.View.AboutModal;
using MinimalTextEditorLite.App.View.Components;
using MinimalTextEditorLite.App.View.ExportModal;
using MinimalTextEditorLite.App.View.SettingsModal;
using MinimalTextEditorLite.Core.Services;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MinimalTextEditorLite.App.ViewModels;

public partial class MainScreenWindowVM : ObservableObject
{
    private readonly NoteService noteService;
    private BackgroundService? backgroundService;
    private EditorControl editorControl = null!;
    private AddOverlayForModals addOverlayForModals = null!;

    public MainScreenWindow MainScreenWindow { get; private set; } = null!;

    public bool SaveNoteCompleted { get; set; }

    [ObservableProperty]
    private object? headerContent;

    [ObservableProperty]
    private object? mainContent;

    [ObservableProperty]
    private string lastSaveTextBlock = string.Empty;

    [ObservableProperty]
    private object? menuContent;

    public MainScreenWindowVM(NoteService noteService)
    {
        this.noteService = noteService;
        SaveNoteCompleted = true;
    }

    public void Attach(MainScreenWindow mainScreen)
    {
        MainScreenWindow = mainScreen;

        editorControl = new EditorControl(this);
        MenuContent = new AppMenuControl(this);
        HeaderContent = new HeaderControl(this);
        MainContent = editorControl;

        var app = (App)Application.Current;
        app.PropertyChanged += App_PropertyChanged;
        LastSaveTextBlock = app.LastNoteUpdated;

        MainScreenWindow.Loaded += MainScreenWindow_Loaded;
        MainScreenWindow.Unloaded += MainScreenWindow_Unloaded;

        SearchForUpdates();
    }

    public void AddToMainGrid()
    {
        editorControl.Visibility = Visibility.Collapsed;

        if (MainScreenWindow.Content is Grid mainGrid)
        {
            addOverlayForModals = new AddOverlayForModals();
            addOverlayForModals.AddOverlayToGrid(mainGrid);
        }
    }

    public void RemoveToMainGrid()
    {
        if (MainScreenWindow.Content is Grid mainGrid)
            addOverlayForModals.RemoveOverlayFromGrid(mainGrid);

        editorControl.Visibility = Visibility.Visible;
    }

    private void App_PropertyChanged(object? sender, System.ComponentModel.PropertyChangedEventArgs e)
    {
        if (e.PropertyName == nameof(App.AutoSaveNote))
        {
            int newInterval = ((App)Application.Current).AutoSaveNote;

            if (newInterval == 0)
            {
                backgroundService?.Stop();
                backgroundService = null;
            }
            else if (backgroundService == null)
            {
                backgroundService = new BackgroundService(SaveNote, newInterval);
                backgroundService.Start();
            }
            else
            {
                backgroundService.UpdateInterval(newInterval);
            }
        }

        if (e.PropertyName == nameof(App.LastNoteUpdated) && sender is App app)
            LastSaveTextBlock = app.LastNoteUpdated;
    }

    private void MainScreenWindow_Loaded(object sender, RoutedEventArgs e)
    {
        int autoSaveInterval = ((App)Application.Current).AutoSaveNote;

        if (autoSaveInterval > 0)
        {
            backgroundService = new BackgroundService(SaveNote, autoSaveInterval);
            backgroundService.Start();
        }

        ((App)Application.Current).PropertyChanged += App_PropertyChanged;
        CheckIfBackupFolderHasReachedSizeLimit();
    }

    private void MainScreenWindow_Unloaded(object sender, RoutedEventArgs e)
    {
        backgroundService?.Dispose();
    }

    private void SearchForUpdates()
    {
        UpdatesCheck updatesCheck = new UpdatesCheck();
        updatesCheck.CheckForUpdates();
    }

    private async void CheckIfBackupFolderHasReachedSizeLimit()
    {
        await Task.Delay(TimeSpan.FromSeconds(3));

        if (((App)Application.Current).ShowBackupSizeLimiteMessage)
        {
            string backupsFolder = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MinimalTextEditorLite",
                "Backups");

            if (Directory.Exists(backupsFolder))
            {
                long folderSize = GetDirectorySize(backupsFolder);
                const long sizeLimit = 600L * 1024 * 1024;

                if (folderSize > sizeLimit)
                {
                    AddToMainGrid();
                    ModalMessages.ShowBackupSizeMessageConfim(
                        App.Localization.Translate("Title_Backup_Limit"),
                        App.Localization.Translate("Description_Backup_Limit"),
                        "");
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
                    totalSize += new FileInfo(file).Length;
                }
                catch
                {
                }
            }
        }
        catch
        {
        }

        return totalSize;
    }

    public void OpenNewNote()
    {
        if (((App)Application.Current).ShowOpenNoteMessage)
        {
            AddToMainGrid();
            bool result = ModalMessages.ShowConfirmOpenNoteModal(
                App.Localization.Translate("Global_Modal_Open_Note_Title"),
                App.Localization.Translate("Global_Modal_Open_Note_Description"),
                App.Localization.Translate("Global_Modal_Open_Note_Bold"));
            RemoveToMainGrid();

            if (!result)
                return;
        }

        if (noteService.OpenNewNote())
            editorControl.LoadCurrentNote();
    }

    [RelayCommand]
    public void SaveNote()
    {
        if (!SaveNoteCompleted)
            return;

        SaveNoteCompleted = false;

        if (Application.Current.Dispatcher.CheckAccess())
            editorControl.DoAction("Save");
        else
            Application.Current.Dispatcher.Invoke(() => editorControl.DoAction("Save"));
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
        SettingsModalWindow settingsModal = new SettingsModalWindow();
        settingsModal.ShowDialog();
        RemoveToMainGrid();
    }

    public void ExportNoteDialog()
    {
        SaveNote();
        AddToMainGrid();

        ExportModalWindow exportModal = new ExportModalWindow();
        bool? dialogResult = exportModal.ShowDialog();

        RemoveToMainGrid();

        if (dialogResult == true)
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

    public void OnPreviewKeyDown(KeyEventArgs e)
    {
        if (e.Key == Key.N && Keyboard.Modifiers == ModifierKeys.Control)
        {
            NewNote();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.O && Keyboard.Modifiers == ModifierKeys.Control)
        {
            OpenNewNote();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.S && Keyboard.Modifiers == ModifierKeys.Control)
        {
            SaveNote();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Delete && Keyboard.Modifiers == ModifierKeys.Control)
        {
            RemoveNote();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.E && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            ExportNoteDialog();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.OemComma && Keyboard.Modifiers == ModifierKeys.Control)
        {
            OpenSettingsDialog();
            e.Handled = true;
        }
    }

    [RelayCommand]
    private void CreateNote() => NewNote();

    [RelayCommand]
    private void OpenNote() => OpenNewNote();

    [RelayCommand]
    private void DeleteNote() => RemoveNote();

    [RelayCommand]
    private void ExportNote() => ExportNoteDialog();

    [RelayCommand]
    private void Settings() => OpenSettingsDialog();

    [RelayCommand]
    private void Exit() => Application.Current.Shutdown();

    [RelayCommand]
    private void About() => OpenAboutApp();
}
