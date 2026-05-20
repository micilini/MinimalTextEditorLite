using Microsoft.Extensions.DependencyInjection;
using MinimalTextEditorLite.App.ViewModels;
using System.Windows;

namespace MinimalTextEditorLite.App.View.MainScreen;

public partial class MainScreenWindow : Window
{
    public MainScreenWindow()
    {
        InitializeComponent();

        var vm = ((App)Application.Current).Services.GetRequiredService<MainScreenWindowVM>();
        vm.Attach(this);

        DataContext = vm;
        PreviewKeyDown += (_, e) => vm.OnPreviewKeyDown(e);
    }
}

