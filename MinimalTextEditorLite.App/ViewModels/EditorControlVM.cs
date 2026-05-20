using CommunityToolkit.Mvvm.ComponentModel;
using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using MinimalTextEditorLite.App.Helpers;
using MinimalTextEditorLite.Core.Database;
using MinimalTextEditorLite.Core.Models;
using System.IO;
using System.Windows;
using WindowsInput;

namespace MinimalTextEditorLite.App.ViewModels;

public partial class EditorControlVM : ObservableObject, IDisposable
{
    private readonly IDatabaseHelper database;
    private readonly NoteService noteService;
    private readonly string virtualHostName = "minimaltexteditorlite.localhost";
    private readonly MainScreenWindowVM mainScreenWindow;
    private readonly WebView2 myWebView;

    private bool userCanSaveNote;
    private bool isFirstTimeLoadingNote = true;

    [ObservableProperty]
    private int progressValue;

    [ObservableProperty]
    private Visibility webViewVisibility = Visibility.Hidden;

    [ObservableProperty]
    private Visibility progressBarVisibility = Visibility.Visible;

    public EditorControlVM(MainScreenWindowVM mainScreen, WebView2 webView, IDatabaseHelper database, NoteService noteService)
    {
        mainScreenWindow = mainScreen;
        myWebView = webView;
        this.database = database;
        this.noteService = noteService;

        InitializeWebView();
    }

    public void Dispose()
    {
    }

    private async void InitializeWebView()
    {
        string userDataFolder = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData),
            "MinimalTextEditorLite",
            "WebView2");

        Directory.CreateDirectory(userDataFolder);

        var env = await CoreWebView2Environment.CreateAsync(null, userDataFolder);
        await myWebView.EnsureCoreWebView2Async(env);

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

        myWebView.CoreWebView2.Navigate($"https://{virtualHostName}/index.html");
        myWebView.CoreWebView2.Settings.AreDefaultContextMenusEnabled = true;
        myWebView.CoreWebView2.Settings.IsWebMessageEnabled = true;
        myWebView.CoreWebView2.Settings.IsZoomControlEnabled = false;
        myWebView.CoreWebView2.Settings.AreDevToolsEnabled = false;
        myWebView.CoreWebView2.NavigationStarting += HandleNavigation;
        myWebView.CoreWebView2.WebMessageReceived += MyWebView_CoreWebView2_WebMessageReceived;
        myWebView.CoreWebView2.NavigationCompleted += async (_, _) =>
        {
            myWebView.CoreWebView2.ContextMenuRequested += MyWebView_CoreWebView2_ContextMenuRequested;
            await Task.Delay(1200);
            LoadCurrentNote();
            isFirstTimeLoadingNote = false;
        };
    }

    private void HandleNavigation(object? sender, CoreWebView2NavigationStartingEventArgs args)
    {
        if (!args.Uri.Equals($"https://{virtualHostName}/index.html", StringComparison.OrdinalIgnoreCase))
        {
            args.Cancel = true;
            myWebView.CoreWebView2.Navigate($"https://{virtualHostName}/index.html");
        }
    }

    public async void SaveContent()
    {
        ProgressBarVisibility = Visibility.Visible;
        await Task.Delay(1200);
        myWebView.CoreWebView2.ExecuteScriptAsync("window.handleSave();");
    }

    public void InsertContent(string noteJson)
    {
        var escapedJson = noteJson.Replace("\\", "\\\\").Replace("'", "\\'");
        myWebView.CoreWebView2.ExecuteScriptAsync($"window.handleLoad('{escapedJson}');");

        ProgressBarVisibility = Visibility.Collapsed;
        WebViewVisibility = Visibility.Visible;
        userCanSaveNote = true;
        mainScreenWindow.SaveNoteCompleted = true;
    }

    private void MyWebView_CoreWebView2_WebMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs e)
    {
        string jsonData = e.TryGetWebMessageAsString();
        UpdateNoteContent(jsonData);
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

    public async void LoadCurrentNote()
    {
        if (!isFirstTimeLoadingNote)
        {
            ProgressBarVisibility = Visibility.Visible;
            WebViewVisibility = Visibility.Collapsed;
            await Task.Delay(1200);
        }

        var currentNote = database.Read<NoteModel>().FirstOrDefault();

        if (currentNote != null)
            InsertContent(currentNote.NoteJson);
        else
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Notes_Not_Found"));
    }

    private void UpdateNoteContent(string jsonData)
    {
        ProgressBarVisibility = Visibility.Visible;

        try
        {
            noteService.UpdateCurrentNote(jsonData);
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
            RemoveCurrentNote();

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
            RemoveCurrentNote();

        mainScreenWindow.RemoveToMainGrid();
    }

    private async void RemoveCurrentNote()
    {
        WebViewVisibility = Visibility.Collapsed;
        ProgressBarVisibility = Visibility.Visible;
        await Task.Delay(1200);

        int rowsAffected = database.Execute("UPDATE Note SET NoteJson = ? WHERE Id = ?", "{\"blocks\": []}", 1);

        if (rowsAffected <= 0)
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_App_Update_Note"));
            return;
        }

        LoadCurrentNote();
    }

    private void OpenSearchBox()
    {
        myWebView.Focus();
        var sim = new InputSimulator();
        sim.Keyboard.ModifiedKeyStroke(WindowsInput.Native.VirtualKeyCode.CONTROL, WindowsInput.Native.VirtualKeyCode.VK_F);
    }
}
