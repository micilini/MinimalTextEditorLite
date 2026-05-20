using Microsoft.Extensions.DependencyInjection;
using MinimalTextEditorLite.App.ViewModels;
using System.Windows;

namespace MinimalTextEditorLite.App.View.SettingsModal;

public partial class SettingsModalWindow : Window
{
    private SettingsModalWindowVM SettingsModalWindowVM { get; }

    public SettingsModalWindow()
    {
        InitializeComponent();

        SettingsModalWindowVM = ActivatorUtilities.CreateInstance<SettingsModalWindowVM>(
            ((App)Application.Current).Services,
            this);

        DataContext = SettingsModalWindowVM;
    }

    private void Window_Loaded(object sender, RoutedEventArgs e)
    {
        SettingsModalWindowVM.AllowUserInteract = true;
    }
}
