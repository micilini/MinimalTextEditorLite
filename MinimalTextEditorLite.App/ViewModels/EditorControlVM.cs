using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using MinimalTextEditorLite.App.Helpers;
using MinimalTextEditorLite.Core.Repositories;
using MinimalTextEditorLite.Core.Security;
using MinimalTextEditorLite.Core.Services;
using System.IO;
using System.Text.Json;
using System.Windows;
using System.Windows.Threading;
using WindowsInput;

namespace MinimalTextEditorLite.App.ViewModels;

public partial class EditorControlVM : ObservableObject, IDisposable
{
    private readonly INoteRepository noteRepository;
    private readonly IBackupService backupService;
    private readonly IEditorJsSecurityService editorJsSecurityService;
    private readonly string virtualHostName = "minimaltexteditorlite.localhost";
    private readonly MainScreenWindowVM mainScreenWindow;
    private readonly WebView2 myWebView;
    private readonly DispatcherTimer statsTimer = new();

    private bool userCanSaveNote;
    private bool isFirstTimeLoadingNote = true;
    private bool saveValidationErrorShown;

    [ObservableProperty]
    private int progressValue;

    [ObservableProperty]
    private Visibility webViewVisibility = Visibility.Hidden;

    [ObservableProperty]
    private Visibility progressBarVisibility = Visibility.Visible;

    public EditorControlVM(
        MainScreenWindowVM mainScreen,
        WebView2 webView,
        INoteRepository noteRepository,
        IBackupService backupService,
        IEditorJsSecurityService editorJsSecurityService)
    {
        mainScreenWindow = mainScreen;
        myWebView = webView;
        this.noteRepository = noteRepository;
        this.backupService = backupService;
        this.editorJsSecurityService = editorJsSecurityService;

        statsTimer.Interval = TimeSpan.FromSeconds(2);
        statsTimer.Tick += (_, _) => _ = RequestStatsSafeAsync();

        _ = InitializeWebViewSafeAsync();
    }

    public void Dispose()
    {
        statsTimer.Stop();
    }

    private async Task InitializeWebViewSafeAsync()
    {
        try
        {
            await InitializeWebViewAsync();
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Editor_Folder"));
        }
    }

    private async Task InitializeWebViewAsync()
    {
        string userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MinimalTextEditorLite",
            "WebView2");

        Directory.CreateDirectory(userDataFolder);

        var options = new CoreWebView2EnvironmentOptions(
            "--disable-http-cache --disk-cache-size=1 --media-cache-size=1");

        var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder, options);
        await myWebView.EnsureCoreWebView2Async(env);

        await DisableWebViewCacheSafeAsync();

        var folderPath = Path.Combine(Directory.GetCurrentDirectory(), "EditorModules", "EditorJS");

        if (!Directory.Exists(folderPath))
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Editor_Folder"));
            return;
        }

        myWebView.CoreWebView2.SetVirtualHostNameToFolderMapping(
            virtualHostName,
            folderPath,
            CoreWebView2HostResourceAccessKind.DenyCors);

        var cacheBuster = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
        myWebView.CoreWebView2.Navigate($"https://{virtualHostName}/index.html?v={cacheBuster}");
        myWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        myWebView.CoreWebView2.Settings.IsWebMessageEnabled = true;
        myWebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
        myWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        myWebView.CoreWebView2.NavigationStarting += HandleNavigation;
        myWebView.CoreWebView2.WebMessageReceived += MyWebView_CoreWebView2_WebMessageReceived;
        myWebView.CoreWebView2.NavigationCompleted += async (_, _) =>
        {
            try
            {
                myWebView.CoreWebView2.ContextMenuRequested += MyWebView_CoreWebView2_ContextMenuRequested;
                await Task.Delay(1200);
                await LoadCurrentNoteAsync();
                isFirstTimeLoadingNote = false;
            }
            catch
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Notes_Not_Found"));
            }
        };
    }

    private void HandleNavigation(object? sender, CoreWebView2NavigationStartingEventArgs args)
    {
        var allowedUri = $"https://{virtualHostName}/index.html";

        if (!args.Uri.StartsWith(allowedUri, StringComparison.OrdinalIgnoreCase))
        {
            args.Cancel = true;

            var cacheBuster = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds();
            myWebView.CoreWebView2.Navigate($"{allowedUri}?v={cacheBuster}");
        }
    }

    private async Task DisableWebViewCacheSafeAsync()
    {
        try
        {
            if (myWebView.CoreWebView2 is null)
                return;

            await myWebView.CoreWebView2.CallDevToolsProtocolMethodAsync("Network.enable", "{}");
            await myWebView.CoreWebView2.CallDevToolsProtocolMethodAsync(
                "Network.setCacheDisabled",
                "{\"cacheDisabled\":true}");

            await myWebView.CoreWebView2.CallDevToolsProtocolMethodAsync(
                "Network.clearBrowserCache",
                "{}");
        }
        catch
        {
        }
    }

    public void SaveContent()
    {
        _ = SaveContentSafeAsync();
    }

    private async Task SaveContentSafeAsync()
    {
        try
        {
            await SaveContentAsync();
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Update_Note"));
        }
    }

    public async Task SaveContentAsync()
    {
        mainScreenWindow.MarkDirty();
        ProgressBarVisibility = Visibility.Visible;
        await Task.Delay(1200);
        await PostEditorMessageAsync(new
        {
            action = "save"
        });
    }

    public void SaveContentDebounced()
    {
        _ = SaveContentDebouncedSafeAsync();
    }

    private async Task SaveContentDebouncedSafeAsync()
    {
        try
        {
            await SaveContentDebouncedAsync();
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Update_Note"));
        }
    }

    public async Task SaveContentDebouncedAsync()
    {
        mainScreenWindow.MarkDirty();
        ProgressBarVisibility = Visibility.Visible;

        await PostEditorMessageAsync(new
        {
            action = "saveDebounced"
        });
    }

    public void InsertContent(string noteJson)
    {
        _ = InsertContentSafeAsync(noteJson);
    }

    private async Task InsertContentSafeAsync(string noteJson)
    {
        try
        {
            await InsertContentAsync(noteJson);
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_App_Update_Note"));
        }
    }

    public async Task InsertContentAsync(string noteJson)
    {
        JsonElement document;

        try
        {
            document = JsonSerializer.Deserialize<JsonElement>(noteJson);
        }
        catch
        {
            document = JsonSerializer.Deserialize<JsonElement>("{\"blocks\":[]}");
        }

        await PostEditorMessageAsync(new
        {
            action = "load",
            data = document
        });

        SendThemeToEditor(((App)Application.Current).EffectiveTheme);

        ProgressBarVisibility = Visibility.Collapsed;
        WebViewVisibility = Visibility.Visible;
        userCanSaveNote = true;
        mainScreenWindow.SaveNoteCompleted = true;

        if (!statsTimer.IsEnabled)
            statsTimer.Start();

        await RequestStatsSafeAsync();
    }

    public void SendThemeToEditor(string? themeName)
    {
        _ = SendThemeToEditorSafeAsync(themeName);
    }

    private async Task SendThemeToEditorSafeAsync(string? themeName)
    {
        try
        {
            await SendThemeToEditorAsync(themeName);
        }
        catch
        {
        }
    }

    public async Task SendThemeToEditorAsync(string? themeName)
    {
        var normalizedTheme = string.Equals(themeName, "dark", StringComparison.OrdinalIgnoreCase)
            ? "dark"
            : "light";

        await PostEditorMessageAsync(new
        {
            action = "setTheme",
            data = normalizedTheme
        });
    }

    private Task PostEditorMessageAsync(object message)
    {
        if (myWebView.CoreWebView2 == null)
            return Task.CompletedTask;

        var payload = JsonSerializer.Serialize(message);
        myWebView.CoreWebView2.PostWebMessageAsJson(payload);
        return Task.CompletedTask;
    }

    private void MyWebView_CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        try
        {
            string rawMessage = e.WebMessageAsJson;

            using var document = JsonDocument.Parse(rawMessage);
            var root = document.RootElement;

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("event", out var eventProperty))
            {
                HandleBridgeEvent(eventProperty.GetString(), root);
                return;
            }

            if (root.ValueKind == JsonValueKind.String)
            {
                var jsonData = root.GetString();

                if (!string.IsNullOrWhiteSpace(jsonData))
                    _ = UpdateNoteContentSafeAsync(jsonData);

                return;
            }

            if (root.ValueKind == JsonValueKind.Object && root.TryGetProperty("blocks", out _))
            {
                _ = UpdateNoteContentSafeAsync(root.GetRawText());
                return;
            }
        }
        catch
        {
            try
            {
                string jsonData = e.TryGetWebMessageAsString();

                if (!string.IsNullOrWhiteSpace(jsonData))
                    _ = UpdateNoteContentSafeAsync(jsonData);
            }
            catch
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Update_Note"));
            }
        }
    }

    private void HandleBridgeEvent(string? eventName, JsonElement root)
    {
        switch (eventName)
        {
            case "bridgeReady":
                _ = RequestStatsSafeAsync();
                break;

            case "stats":
                HandleStatsEvent(root);
                break;

            case "dirty":
                Application.Current.Dispatcher.Invoke(mainScreenWindow.MarkDirty);
                break;
        }
    }

    private void HandleStatsEvent(JsonElement root)
    {
        if (!root.TryGetProperty("data", out var data))
            return;

        var words = data.TryGetProperty("words", out var wordsElement) && wordsElement.TryGetInt32(out var wordsValue)
            ? wordsValue
            : 0;

        var readingTime = data.TryGetProperty("readingTimeMin", out var readingElement) && readingElement.TryGetInt32(out var readingValue)
            ? readingValue
            : 1;

        Application.Current.Dispatcher.Invoke(() =>
        {
            mainScreenWindow.UpdateEditorStats(words, readingTime);
        });
    }

    private async Task RequestStatsSafeAsync()
    {
        try
        {
            await RequestStatsAsync();
        }
        catch
        {
        }
    }

    private async Task RequestStatsAsync()
    {
        await PostEditorMessageAsync(new
        {
            action = "getStats"
        });
    }

    private void MyWebView_CoreWebView2_ContextMenuRequested(object? sender, CoreWebView2ContextMenuRequestedEventArgs args)
    {
        IList<CoreWebView2ContextMenuItem> itemsToRemove = new List<CoreWebView2ContextMenuItem>();

        for (int index = 0; index < args.MenuItems.Count; index++)
        {
            if (args.MenuItems[index].Name is "back" or "reload" or "saveAs" or "saveImageAs" or "share" or "print" or "forward" or "openLinkInNewWindow" or "webCapture")
                itemsToRemove.Add(args.MenuItems[index]);
        }

        foreach (var item in itemsToRemove)
            args.MenuItems.Remove(item);
    }

    public void LoadCurrentNote()
    {
        _ = LoadCurrentNoteSafeAsync();
    }

    private async Task LoadCurrentNoteSafeAsync()
    {
        try
        {
            await LoadCurrentNoteAsync();
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Notes_Not_Found"));
        }
    }

    public async Task LoadCurrentNoteAsync()
    {
        if (!isFirstTimeLoadingNote)
        {
            ProgressBarVisibility = Visibility.Visible;
            WebViewVisibility = Visibility.Collapsed;
            await Task.Delay(1200);
        }

        var currentNote = await noteRepository.GetCurrentAsync();

        if (currentNote != null)
        {
            var validation = await editorJsSecurityService.ValidateAndNormalizeJsonAsync(currentNote.NoteJson);
            if (!validation.Success || string.IsNullOrWhiteSpace(validation.NormalizedJson))
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Invalid_Json"));
                await InsertContentAsync("{\"blocks\":[]}");
                return;
            }

            if (!string.Equals(currentNote.NoteJson, validation.NormalizedJson, StringComparison.Ordinal))
                await noteRepository.UpdateJsonAsync(validation.NormalizedJson);

            await InsertContentAsync(validation.NormalizedJson);
            mainScreenWindow.MarkClean();
        }
        else
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Notes_Not_Found"));
        }
    }

    private async Task UpdateNoteContentSafeAsync(string jsonData)
    {
        try
        {
            await UpdateNoteContentAsync(jsonData);
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Update_Note"));
        }
    }

    private async Task UpdateNoteContentAsync(string jsonData)
    {
        ProgressBarVisibility = Visibility.Visible;

        try
        {
            var validation = await editorJsSecurityService.ValidateAndNormalizeJsonAsync(jsonData);
            if (!validation.Success || string.IsNullOrWhiteSpace(validation.NormalizedJson))
            {
                if (!saveValidationErrorShown)
                {
                    ModalMessages.showErrorModal(App.Localization.Translate("Error_Invalid_Json"));
                    saveValidationErrorShown = true;
                }

                return;
            }

            jsonData = validation.NormalizedJson;
            var updateSuccess = await noteRepository.UpdateJsonAsync(jsonData);

            if (updateSuccess)
            {
                saveValidationErrorShown = false;
                string currentDate = ((App)Application.Current).AppLanguage == "pt_br"
                    ? DateTime.Now.ToString("dd/MM/yyyy H:mm:ss")
                    : DateTime.Now.ToString("MM/dd/yyyy h:mm:ss tt");

                ((App)Application.Current).LastNoteUpdated = App.Localization.Translate("Last_Save_Note") + currentDate;
                await backupService.CreateBackupAsync(jsonData, currentDate);
                mainScreenWindow.MarkClean();
                await RequestStatsSafeAsync();
            }
            else
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_App_Update_Note"));
            }
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Update_Note"));
        }
        finally
        {
            ProgressBarVisibility = Visibility.Collapsed;
            mainScreenWindow.SaveNoteCompleted = true;
        }
    }

    public void DoAction(string action)
    {
        switch (action)
        {
            case "Save":
                Save_Action();
                break;
            case "SaveDebounced":
                Save_Debounced_Action();
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
        }
    }

    private void Save_Action()
    {
        if (userCanSaveNote)
            SaveContent();
    }

    private void Save_Debounced_Action()
    {
        if (userCanSaveNote)
            SaveContentDebounced();
    }

    private void Delete_Action()
    {
        AskIfUserWillDeleteThisNote();
    }

    private void New_Action()
    {
        mainScreenWindow.AddToMainGrid();

        bool result = ModalMessages.ShowConfirmModal(
            App.Localization.Translate("Confirm_New_Reset_Note_Title"),
            App.Localization.Translate("Confirm_New_Reset_Note_Description"),
            App.Localization.Translate("Confirm_Reset_Note_Bold"));

        if (result)
            _ = RemoveCurrentNoteSafeAsync();

        mainScreenWindow.RemoveToMainGrid();
    }

    private void AskIfUserWillDeleteThisNote()
    {
        mainScreenWindow.AddToMainGrid();

        bool result = ModalMessages.ShowConfirmModal(
            App.Localization.Translate("Confirm_Reset_Note_Title"),
            App.Localization.Translate("Confirm_Reset_Note_Description"),
            App.Localization.Translate("Confirm_Reset_Note_Bold"));

        if (result)
            _ = RemoveCurrentNoteSafeAsync();

        mainScreenWindow.RemoveToMainGrid();
    }

    private async Task RemoveCurrentNoteSafeAsync()
    {
        try
        {
            await RemoveCurrentNoteAsync();
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_App_Update_Note"));
        }
    }

    private async Task RemoveCurrentNoteAsync()
    {
        WebViewVisibility = Visibility.Collapsed;
        ProgressBarVisibility = Visibility.Visible;
        await Task.Delay(1200);

        bool cleared = await noteRepository.ClearAsync();

        if (!cleared)
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_App_Update_Note"));
            return;
        }

        await LoadCurrentNoteAsync();
    }

    private void OpenSearchBox()
    {
        myWebView.Focus();
        var sim = new InputSimulator();
        sim.Keyboard.ModifiedKeyStroke(WindowsInput.Native.VirtualKeyCode.CONTROL, WindowsInput.Native.VirtualKeyCode.VK_F);
    }
}

