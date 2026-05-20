using Microsoft.Extensions.DependencyInjection;
using MinimalTextEditorLite.App.ViewModels;
using MinimalTextEditorLite.Core.Models;
using System.Windows;

namespace MinimalTextEditorLite.App.View.GlobalModals;

public partial class GlobalModalsWindow : Window
{
    private GlobalModalsWindowVM GlobalModalsWindowVM { get; }
    private string TypeMessage { get; }

    public GlobalModalsWindow(GlobalModalModel gmm, bool inputModal, string typeMessage)
    {
        InitializeComponent();

        TypeMessage = typeMessage;
        GlobalModalsWindowVM = ActivatorUtilities.CreateInstance<GlobalModalsWindowVM>(
            ((App)Application.Current).Services,
            this,
            gmm,
            inputModal);

        DataContext = GlobalModalsWindowVM;
    }

    private void CheckBox_Checked(object sender, RoutedEventArgs e)
    {
        ((App)Application.Current).ShowOpenNoteMessage = false;

        if (TypeMessage == "OpenNoteMessage")
        {
            GlobalModalsWindowVM.UpdateShowOpenNoteMessage(false);
            return;
        }

        if (TypeMessage == "BackupMessage")
            GlobalModalsWindowVM.UpdateShowBackupSizeMessage(false);
    }

    private void CheckBox_Unchecked(object sender, RoutedEventArgs e)
    {
        ((App)Application.Current).ShowOpenNoteMessage = true;

        if (TypeMessage == "OpenNoteMessage")
            GlobalModalsWindowVM.UpdateShowOpenNoteMessage(true);

        if (TypeMessage == "BackupMessage")
            GlobalModalsWindowVM.UpdateShowBackupSizeMessage(true);
    }
}
