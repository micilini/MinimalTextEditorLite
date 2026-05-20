using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;

namespace MinimalTextEditorLite.App.ViewModels;

public partial class HeaderControlVM : ObservableObject
{
    private readonly MainScreenWindowVM mainScreenWindow;

    public HeaderControlVM(MainScreenWindowVM mainScreen)
    {
        mainScreenWindow = mainScreen;
    }

    [RelayCommand]
    private void OpenNote()
    {
        mainScreenWindow.OpenNewNote();
    }

    [RelayCommand]
    private void SaveNote()
    {
        mainScreenWindow.SaveNote();
    }

    [RelayCommand]
    private void RemoveNote()
    {
        mainScreenWindow.RemoveNote();
    }

    [RelayCommand]
    private void Settings()
    {
        mainScreenWindow.OpenSettingsDialog();
    }

    [RelayCommand]
    private void Export()
    {
        mainScreenWindow.ExportNoteDialog();
    }

    [RelayCommand]
    private void SearchNote()
    {
        mainScreenWindow.SearchNote();
    }
}
