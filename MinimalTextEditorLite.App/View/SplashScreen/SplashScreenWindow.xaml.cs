using Microsoft.Extensions.DependencyInjection;
using MinimalTextEditorLite.App.View.MainScreen;
using MinimalTextEditorLite.App.ViewModels;
using System.Windows;

namespace MinimalTextEditorLite.App.View.SplashScreen;

public partial class SplashScreenWindow : Window
{
    private readonly SplashScreenWindowVM viewModel;

    public SplashScreenWindow()
    {
        InitializeComponent();
        viewModel = ((App)Application.Current).Services.GetRequiredService<SplashScreenWindowVM>();
        DataContext = viewModel;
        viewModel.OnLoadingComplete += OnLoadingComplete;
    }

    private async void Window_ContentRendered(object sender, EventArgs e)
    {
        await viewModel.InitializeAsync();
    }

    private void OnLoadingComplete()
    {
        Application.Current.Dispatcher.Invoke(() =>
        {
            MainScreenWindow mainScreen = new MainScreenWindow();
            mainScreen.Show();
            Close();
        });
    }
}

