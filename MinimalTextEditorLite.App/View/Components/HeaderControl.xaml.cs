using Microsoft.Extensions.DependencyInjection;
using MinimalTextEditorLite.App.ViewModels;
using System.Windows;
using System.Windows.Controls;

namespace MinimalTextEditorLite.App.View.Components;

public partial class HeaderControl : UserControl
{
    public MainScreenWindowVM MainScreenWindow { get; }

    public HeaderControl(MainScreenWindowVM MS)
    {
        InitializeComponent();
        MainScreenWindow = MS;

        DataContext = ActivatorUtilities.CreateInstance<HeaderControlVM>(
            ((App)Application.Current).Services,
            MS);
    }
}
