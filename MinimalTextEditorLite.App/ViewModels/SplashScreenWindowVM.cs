using CommunityToolkit.Mvvm.ComponentModel;

namespace MinimalTextEditorLite.App.ViewModels;

public partial class SplashScreenWindowVM : ObservableObject
{
    public event Action? OnLoadingComplete;

    [ObservableProperty]
    private int progressValue;

    // Database/settings initialization already runs in App.InitializeApplicationStateBeforeSplash()
    // before the SplashScreen is created. This VM only drives the visual loading transition.
    public async Task InitializeAsync()
    {
        await Task.Run(() =>
        {
            for (var i = 0; i <= 100; i++)
            {
                ProgressValue = i;
                Thread.Sleep(20);
            }
        });

        OnLoadingComplete?.Invoke();
    }
}
