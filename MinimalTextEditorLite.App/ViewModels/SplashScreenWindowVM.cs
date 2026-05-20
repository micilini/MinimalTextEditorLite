using CommunityToolkit.Mvvm.ComponentModel;
using MinimalTextEditorLite.Core.Startup;
using System.Windows;

namespace MinimalTextEditorLite.App.ViewModels;

public partial class SplashScreenWindowVM(StartupAppConfiguration startupConfig) : ObservableObject
{
    public event Action? OnLoadingComplete;

    [ObservableProperty]
    private int progressValue;

    public async Task InitializeAsync()
    {
        await Task.Run(() =>
        {
            var result = startupConfig.CheckAndCreateDatabase();

            Application.Current.Dispatcher.Invoke(() =>
            {
                ((App)Application.Current).ApplySettings(result.Settings);
            });

            for (var i = 0; i <= 100; i++)
            {
                ProgressValue = i;
                Thread.Sleep(20);
            }
        });

        OnLoadingComplete?.Invoke();
    }
}
