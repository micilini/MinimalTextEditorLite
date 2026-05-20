using System.Windows;
using System.Windows.Input;

namespace MinimalTextEditorLite.App.View.AboutModal;

public partial class AboutModalWindow : Window
{
    public AboutModalWindow()
    {
        InitializeComponent();

        AppId.Text = ((App)Application.Current).ApplicationIdentifier;
        AppV.Text = ((App)Application.Current).ApplicationVersion;
    }

    private void OnCloseIconClicked(object sender, MouseButtonEventArgs e)
    {
        Close();
    }
}
