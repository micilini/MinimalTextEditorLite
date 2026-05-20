using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using Microsoft.Win32;
using MinimalTextEditorLite.App.Helpers;
using MinimalTextEditorLite.App.View;
using MinimalTextEditorLite.App.View.AboutModal;
using MinimalTextEditorLite.App.View.Components;
using MinimalTextEditorLite.App.View.ExportModal;
using MinimalTextEditorLite.App.View.MetadataModal;
using MinimalTextEditorLite.App.View.SettingsModal;
using MinimalTextEditorLite.Core.Models;
using MinimalTextEditorLite.Core.Repositories;
using MinimalTextEditorLite.Core.Services;
using System.Collections.ObjectModel;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;

namespace MinimalTextEditorLite.App.ViewModels;

public partial class MainScreenWindowVM : ObservableObject
{
    private readonly IImportService importService;
    private readonly IRecentFilesRepository recentFilesRepository;
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

    [ObservableProperty]
    private bool isFocusMode;

    partial void OnIsFocusModeChanged(bool value)
    {
        HeaderVisibility = value ? Visibility.Collapsed : Visibility.Visible;
        HeaderRowHeight = value ? new GridLength(0) : new GridLength(80);

        FocusModeFooterVisibility = value ? Visibility.Visible : Visibility.Collapsed;
        FocusModeFooterText = value
            ? App.Localization.Translate("Focus_Mode_Footer_Message")
            : string.Empty;
    }

    [ObservableProperty]
    private Visibility headerVisibility = Visibility.Visible;

    [ObservableProperty]
    private GridLength headerRowHeight = new(80);

    [ObservableProperty]
    private Visibility focusModeFooterVisibility = Visibility.Collapsed;

    [ObservableProperty]
    private string focusModeFooterText = string.Empty;

    [ObservableProperty]
    private int wordCount;

    partial void OnWordCountChanged(int value)
    {
        UpdateFooterStatusText();
    }

    [ObservableProperty]
    private int readingTimeMinutes = 1;

    partial void OnReadingTimeMinutesChanged(int value)
    {
        UpdateFooterStatusText();
    }

    [ObservableProperty]
    private string footerStatusText = string.Empty;

    [ObservableProperty]
    private bool isDirty;

    partial void OnIsDirtyChanged(bool value)
    {
        WindowTitle = value
            ? "* MinimalTextEditor"
            : "MinimalTextEditor";
    }

    [ObservableProperty]
    private string windowTitle = "MinimalTextEditor";

    public ObservableCollection<RecentFileModel> RecentFiles { get; } = new();

    partial void OnLastSaveTextBlockChanged(string value)
    {
        UpdateFooterStatusText();
    }

    public MainScreenWindowVM(IImportService importService, IRecentFilesRepository recentFilesRepository)
    {
        this.importService = importService;
        this.recentFilesRepository = recentFilesRepository;
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
        UpdateFooterStatusText();

        MainScreenWindow.Loaded += MainScreenWindow_Loaded;
        MainScreenWindow.Unloaded += MainScreenWindow_Unloaded;

        _ = LoadRecentFilesSafeAsync();
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
                backgroundService = new BackgroundService(AutoSaveNote, newInterval);
                backgroundService.Start();
            }
            else
            {
                backgroundService.UpdateInterval(newInterval);
            }
        }

        if (e.PropertyName == nameof(App.LastNoteUpdated) && sender is App app)
            LastSaveTextBlock = app.LastNoteUpdated;

        if (e.PropertyName == nameof(App.EffectiveTheme) && sender is App themeApp)
            editorControl.SendThemeToEditor(themeApp.EffectiveTheme);
    }

    private void MainScreenWindow_Loaded(object sender, RoutedEventArgs e)
    {
        int autoSaveInterval = ((App)Application.Current).AutoSaveNote;

        if (autoSaveInterval > 0)
        {
            backgroundService = new BackgroundService(AutoSaveNote, autoSaveInterval);
            backgroundService.Start();
        }

        ((App)Application.Current).PropertyChanged += App_PropertyChanged;
        _ = CheckIfBackupFolderHasReachedSizeLimitSafeAsync();
        _ = OpenPendingFileSafeAsync();
    }

    private void MainScreenWindow_Unloaded(object sender, RoutedEventArgs e)
    {
        backgroundService?.Dispose();
    }

    private async Task CheckIfBackupFolderHasReachedSizeLimitSafeAsync()
    {
        try
        {
            await CheckIfBackupFolderHasReachedSizeLimitAsync();
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Backup_Folder_Stats"));
        }
    }

    private async Task CheckIfBackupFolderHasReachedSizeLimitAsync()
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

    private async Task LoadRecentFilesSafeAsync()
    {
        try
        {
            await LoadRecentFilesAsync();
        }
        catch
        {
            // Recent files are non-critical.
        }
    }

    private async Task LoadRecentFilesAsync()
    {
        var files = await recentFilesRepository.GetAllAsync();

        RecentFiles.Clear();

        foreach (var file in files)
            RecentFiles.Add(file);
    }

    public async Task OpenRecentFileAsync(RecentFileModel recentFile)
    {
        if (recentFile == null)
            return;

        if (!File.Exists(recentFile.FilePath))
        {
            AddToMainGrid();
            var remove = ModalMessages.ShowConfirmModal(
                App.Localization.Translate("Recent_File_Not_Found_Title"),
                App.Localization.Translate("Recent_File_Not_Found_Description"),
                recentFile.FileName);
            RemoveToMainGrid();

            if (remove)
            {
                await recentFilesRepository.RemoveAsync(recentFile.FilePath);
                await LoadRecentFilesAsync();
            }

            return;
        }

        var importResult = await importService.ImportAsync(recentFile.FilePath);

        if (!importResult.Success)
        {
            ModalMessages.showErrorModal(App.Localization.Translate(importResult.ErrorKey ?? "Error_Note_Import"));
            return;
        }

        editorControl.LoadCurrentNote();
        await LoadRecentFilesAsync();
    }

    private async Task OpenPendingFileSafeAsync()
    {
        try
        {
            if (Application.Current is not App { PendingFileToOpen: { } path })
                return;

            if (!File.Exists(path))
                return;

            await Task.Delay(TimeSpan.FromSeconds(2));

            var importResult = await importService.ImportAsync(path);

            if (!importResult.Success)
            {
                ModalMessages.showErrorModal(App.Localization.Translate(importResult.ErrorKey ?? "Error_Note_Import"));
                return;
            }

            editorControl.LoadCurrentNote();
            await LoadRecentFilesAsync();
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Note_Import"));
        }
    }

    public void UpdateEditorStats(int words, int readingTimeMinutes)
    {
        WordCount = Math.Max(0, words);
        ReadingTimeMinutes = WordCount == 0 ? 0 : Math.Max(1, readingTimeMinutes);
    }

    private void UpdateFooterStatusText()
    {
        var statsText = string.Format(
            App.Localization.Translate("Footer_Stats_Text"),
            WordCount,
            ReadingTimeMinutes);

        FooterStatusText = string.IsNullOrWhiteSpace(LastSaveTextBlock)
            ? statsText
            : $"{LastSaveTextBlock}     ·     {statsText}";
    }

    public void MarkDirty()
    {
        IsDirty = true;
    }

    public void MarkClean()
    {
        IsDirty = false;
    }

    public void ToggleFocusMode()
    {
        if (IsFocusMode)
        {
            ExitFocusMode();
            return;
        }

        IsFocusMode = true;
    }

    public void ExitFocusMode()
    {
        IsFocusMode = false;
    }

    private void ToggleFullscreen()
    {
        if (MainScreenWindow.WindowState == WindowState.Maximized &&
            MainScreenWindow.WindowStyle == WindowStyle.None)
        {
            MainScreenWindow.WindowStyle = WindowStyle.SingleBorderWindow;
            MainScreenWindow.WindowState = WindowState.Normal;
            return;
        }

        MainScreenWindow.WindowStyle = WindowStyle.None;
        MainScreenWindow.WindowState = WindowState.Maximized;
    }

    public void OpenNewNote()
    {
        _ = OpenNewNoteSafeAsync();
    }

    private async Task OpenNewNoteSafeAsync()
    {
        try
        {
            await OpenNewNoteAsync();
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Note_Import"));
        }
    }

    public async Task OpenNewNoteAsync()
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

        var openFileDialog = new OpenFileDialog
        {
            Filter = "Supported Files (*.json;*.md)|*.json;*.md|JSON Files (*.json)|*.json|Markdown Files (*.md)|*.md",
            DefaultExt = "json",
            Title = App.Localization.Translate("Title_Open_Note")
        };

        if (openFileDialog.ShowDialog() != true)
            return;

        var importResult = await importService.ImportAsync(openFileDialog.FileName);

        if (!importResult.Success)
        {
            ModalMessages.showErrorModal(App.Localization.Translate(importResult.ErrorKey ?? "Error_Note_Import"));
            return;
        }

        editorControl.LoadCurrentNote();
        await LoadRecentFilesAsync();
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

    private void AutoSaveNote()
    {
        if (!SaveNoteCompleted)
            return;

        SaveNoteCompleted = false;

        if (Application.Current.Dispatcher.CheckAccess())
            editorControl.SaveContentDebounced();
        else
            Application.Current.Dispatcher.Invoke(() => editorControl.SaveContentDebounced());
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

    public void OpenMetadataDialog()
    {
        AddToMainGrid();
        var metadataModal = new MetadataModalWindow();
        metadataModal.ShowDialog();
        RemoveToMainGrid();
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

        if (e.Key == Key.E && Keyboard.Modifiers == ModifierKeys.Control)
        {
            ExportNoteDialog();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F && Keyboard.Modifiers == (ModifierKeys.Control | ModifierKeys.Shift))
        {
            ToggleFocusMode();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.Escape && IsFocusMode)
        {
            ExitFocusMode();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.F11 && Keyboard.Modifiers == ModifierKeys.None)
        {
            ToggleFullscreen();
            e.Handled = true;
            return;
        }

        if (e.Key == Key.OemComma && Keyboard.Modifiers == ModifierKeys.Control)
        {
            OpenSettingsDialog();
            e.Handled = true;
            return;
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
    private void FocusMode() => ToggleFocusMode();

    [RelayCommand]
    private async Task OpenRecentFile(RecentFileModel recentFile)
    {
        await OpenRecentFileAsync(recentFile);
    }

    [RelayCommand]
    private void Metadata() => OpenMetadataDialog();

    [RelayCommand]
    private void Settings() => OpenSettingsDialog();

    [RelayCommand]
    private void Exit() => Application.Current.Shutdown();

    [RelayCommand]
    private void About() => OpenAboutApp();
}


