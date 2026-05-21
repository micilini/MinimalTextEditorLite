using Microsoft.Extensions.DependencyInjection;
using MinimalTextEditorLite.App.Helpers;
using MinimalTextEditorLite.App.Services;
using MinimalTextEditorLite.App.ViewModels;
using MinimalTextEditorLite.Core.Database;
using MinimalTextEditorLite.Core.Exporters;
using MinimalTextEditorLite.Core.Importers;
using MinimalTextEditorLite.Core.Models;
using MinimalTextEditorLite.Core.Rendering;
using MinimalTextEditorLite.Core.Repositories;
using MinimalTextEditorLite.Core.Security;
using MinimalTextEditorLite.Core.Services;
using MinimalTextEditorLite.Core.Startup;
using System.ComponentModel;
using System.IO;
using System.Windows;

namespace MinimalTextEditorLite.App;

public partial class App : Application, INotifyPropertyChanged
{
    private static Mutex? _appMutex;

    public event PropertyChangedEventHandler? PropertyChanged;

    public IServiceProvider Services { get; private set; } = null!;

    public StartupResult? StartupResult { get; private set; }

    public string? PendingFileToOpen { get; private set; }

    public static LocalizationHelper Localization { get; private set; } = null!;

    public string DatabaseFileName { get; } = "mte-lite.dll";

    public string ApplicationVersion { get; } = "2.2.1";

    public string ApplicationIdentifier { get; set; } = string.Empty;

    private int autoSaveNote;
    public int AutoSaveNote
    {
        get => autoSaveNote;
        set
        {
            if (autoSaveNote == value)
                return;

            autoSaveNote = value;
            OnPropertyChanged(nameof(AutoSaveNote));
        }
    }

    public string AppLanguage { get; set; } = "en_us";

    public bool ShowBackupSizeLimiteMessage { get; set; }

    public bool ShowOpenNoteMessage { get; set; }

    private string themePreference = AppThemePreference.Light;
    public string ThemePreference
    {
        get => themePreference;
        set
        {
            if (themePreference == value)
                return;

            themePreference = value;
            OnPropertyChanged(nameof(ThemePreference));
        }
    }

    private string effectiveTheme = AppThemePreference.Light;
    public string EffectiveTheme
    {
        get => effectiveTheme;
        set
        {
            if (effectiveTheme == value)
                return;

            effectiveTheme = value;
            OnPropertyChanged(nameof(EffectiveTheme));
        }
    }

    private string lastNoteUpdated = string.Empty;
    public string LastNoteUpdated
    {
        get => lastNoteUpdated;
        set
        {
            if (lastNoteUpdated == value)
                return;

            lastNoteUpdated = value;
            OnPropertyChanged(nameof(LastNoteUpdated));
        }
    }

    protected override void OnStartup(StartupEventArgs e)
    {
        const string appName = @"Local\Minimal Text Editor Lite";

        _appMutex = new Mutex(true, appName, out var createdNew);

        if (!createdNew)
        {
            Environment.Exit(0);
            return;
        }


        CreateApplicationFolderIfNeeded();

        LocalizationHelper.CreateLocalizationFileIfNeeded("en_us");
        Localization = new LocalizationHelper(AppLanguage);

        var services = new ServiceCollection();
        ConfigureServices(services);
        Services = services.BuildServiceProvider();

        if (e.Args.Length > 0 && File.Exists(e.Args[0]))
            PendingFileToOpen = e.Args[0];

        InitializeApplicationStateBeforeSplash();

        base.OnStartup(e);
    }

    protected override void OnExit(ExitEventArgs e)
    {
        _appMutex?.Dispose();
        _appMutex = null;

        base.OnExit(e);
    }

    private void ConfigureServices(IServiceCollection services)
    {
        services.AddSingleton(new DatabaseOptions
        {
            DatabaseFileName = DatabaseFileName
        });

        services.AddSingleton<ISqliteConnectionFactory, SqliteConnectionFactory>();
        services.AddSingleton<IDpapiKeyStore, DpapiKeyStore>();
        services.AddSingleton<DatabaseKeyMigrationService>();
        services.AddSingleton<StartupAppConfiguration>();

        services.AddSingleton<INoteRepository, NoteRepository>();
        services.AddSingleton<ISettingsRepository, SettingsRepository>();
        services.AddSingleton<IRecentFilesRepository, RecentFilesRepository>();

        services.AddSingleton<IBackupService, BackupService>();
        services.AddSingleton<IImportService, ImportService>();
        services.AddSingleton<IExportService, ExportService>();
        services.AddSingleton<IThemeService, ThemeService>();
        services.AddSingleton<IEditorJsSecurityService, EditorJsSecurityService>();
        services.AddSingleton<EditorJsImageValidator>();
        services.AddSingleton<EditorJsInlineHtmlSanitizer>();
        services.AddSingleton<IIsolatedTempFileService, IsolatedTempFileService>();
        services.AddSingleton<IPdfRenderer, WebView2PdfRenderer>();

        services.AddSingleton<HtmlDocumentBuilder>();

        services.AddSingleton<IImporter, JsonImporter>();
        services.AddSingleton<IImporter, MarkdownImporter>();

        services.AddSingleton<IExporter, JsonExporter>();
        services.AddSingleton<IExporter, HtmlExporter>();
        services.AddSingleton<IExporter, MarkdownExporter>();
        services.AddSingleton<IExporter, DocExporter>();
        services.AddSingleton<IExporter, PdfExporter>();

        services.AddTransient<SplashScreenWindowVM>();
        services.AddTransient<MainScreenWindowVM>();
        services.AddTransient<EditorControlVM>();
        services.AddTransient<HeaderControlVM>();
        services.AddTransient<SettingsModalWindowVM>();
        services.AddTransient<MetadataModalWindowVM>();
        services.AddTransient<GlobalModalsWindowVM>();
    }

    public void ApplySettings(SettingsModel settings)
    {
        ApplicationIdentifier = settings.ApplicationIdentifier;
        AutoSaveNote = settings.AutoSaveNote;
        AppLanguage = settings.Language;
        ShowBackupSizeLimiteMessage = settings.ShowBackupSizeLimiteMessage;
        ShowOpenNoteMessage = settings.ShowOpenNoteMessage;

        Services.GetRequiredService<IThemeService>().Apply(settings.Theme);
    }

    private void InitializeApplicationStateBeforeSplash()
    {
        try
        {
            StartupResult = Services
                .GetRequiredService<StartupAppConfiguration>()
                .CheckAndCreateDatabase();

            ApplySettings(StartupResult.Settings);
        }
        catch
        {
            ThemePreference = AppThemePreference.Light;
            EffectiveTheme = AppThemePreference.Light;
        }
    }

    private static void CreateApplicationFolderIfNeeded()
    {
        Directory.CreateDirectory(AppPaths.LocalAppDataFolder);
        Directory.CreateDirectory(AppPaths.BackupsFolder);
    }

    private void OnPropertyChanged(string propertyName)
    {
        PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
    }
}



