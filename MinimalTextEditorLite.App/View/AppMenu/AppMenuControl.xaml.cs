using MinimalTextEditorLite.App.ViewModels;
using MinimalTextEditorLite.Core.Models;
using System.Windows;
using System.Windows.Controls;

namespace MinimalTextEditorLite.App.View.AppMenu;

public partial class AppMenuControl : UserControl
{
    private MainScreenWindowVM MainScreenWindowVM { get; }

    public AppMenuControl(MainScreenWindowVM mainScreen)
    {
        InitializeComponent();
        MainScreenWindowVM = mainScreen;
        DataContext = mainScreen;
    }

    private async void RecentFileMenuItem_Click(object sender, RoutedEventArgs e)
    {
        if (sender is not MenuItem { DataContext: RecentFileModel recentFile })
            return;

        await MainScreenWindowVM.OpenRecentFileAsync(recentFile);
    }
}
