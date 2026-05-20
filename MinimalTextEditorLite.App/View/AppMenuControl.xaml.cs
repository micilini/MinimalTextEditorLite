using MinimalTextEditorLite.App.ViewModels;
using System.Windows.Controls;

namespace MinimalTextEditorLite.App.View;

public partial class AppMenuControl : UserControl
{
    private MainScreenWindowVM MainScreenWindowVM { get; }

    public AppMenuControl(MainScreenWindowVM mainScreen)
    {
        InitializeComponent();
        MainScreenWindowVM = mainScreen;
        DataContext = mainScreen;
    }
}
