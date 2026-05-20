using Microsoft.Extensions.DependencyInjection;
using MinimalTextEditorLite.App.ViewModels;
using System.Windows.Controls;

namespace MinimalTextEditorLite.App.View;

public partial class EditorControl : UserControl
{
    private EditorControlVM EditorControlVM { get; }

    public EditorControl(MainScreenWindowVM mainScreen)
    {
        InitializeComponent();

        EditorControlVM = ActivatorUtilities.CreateInstance<EditorControlVM>(
            ((App)System.Windows.Application.Current).Services,
            mainScreen,
            myWebView);

        DataContext = EditorControlVM;
    }

    public void LoadCurrentNote()
    {
        EditorControlVM.LoadCurrentNote();
    }

    public void SaveContentDebounced()
    {
        EditorControlVM.SaveContentDebounced();
    }

    public void SendThemeToEditor(string? themeName)
    {
        EditorControlVM.SendThemeToEditor(themeName);
    }

    public void DoAction(string action)
    {
        EditorControlVM.DoAction(action);
    }
}
