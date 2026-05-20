using Microsoft.Extensions.DependencyInjection;
using MinimalTextEditorLite.App.ViewModels;
using System.Windows;

namespace MinimalTextEditorLite.App.View.MetadataModal;

public partial class MetadataModalWindow : Window
{
    private MetadataModalWindowVM MetadataModalWindowVM { get; }

    public MetadataModalWindow()
    {
        InitializeComponent();

        MetadataModalWindowVM = ActivatorUtilities.CreateInstance<MetadataModalWindowVM>(
            ((App)Application.Current).Services,
            this);

        DataContext = MetadataModalWindowVM;
    }

    private async void Window_Loaded(object sender, RoutedEventArgs e)
    {
        await MetadataModalWindowVM.LoadAsync();
    }
}
