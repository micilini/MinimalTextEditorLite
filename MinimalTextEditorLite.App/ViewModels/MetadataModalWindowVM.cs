using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MinimalTextEditorLite.App.Helpers;
using MinimalTextEditorLite.App.View.MetadataModal;
using MinimalTextEditorLite.Core.Models;
using MinimalTextEditorLite.Core.Repositories;

namespace MinimalTextEditorLite.App.ViewModels;

public partial class MetadataModalWindowVM : ObservableObject
{
    private readonly INoteRepository noteRepository;
    private readonly MetadataModalWindow metadataModalWindow;

    [ObservableProperty]
    private string title = string.Empty;

    [ObservableProperty]
    private string slug = string.Empty;

    [ObservableProperty]
    private string tags = string.Empty;

    [ObservableProperty]
    private DateTime? publishDate;

    public MetadataModalWindowVM(MetadataModalWindow metadataModalWindow, INoteRepository noteRepository)
    {
        this.metadataModalWindow = metadataModalWindow;
        this.noteRepository = noteRepository;
    }

    public async Task LoadAsync()
    {
        var note = await noteRepository.GetCurrentAsync();
        if (note == null)
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Notes_Not_Found"));
            return;
        }

        Title = note.Title ?? string.Empty;
        Slug = note.Slug ?? string.Empty;
        Tags = note.Tags ?? string.Empty;
        PublishDate = note.PublishDate;
    }

    [RelayCommand]
    private async Task Save()
    {
        var metadata = new NoteMetadata
        {
            Title = Title,
            Slug = Slug,
            Tags = Tags,
            PublishDate = PublishDate
        };

        var updated = await noteRepository.UpdateMetadataAsync(metadata);

        if (!updated)
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Metadata_Update"));
            return;
        }

        ModalMessages.ShowSuccessModal(
            App.Localization.Translate("Metadata_Save_Success_Title"),
            App.Localization.Translate("Metadata_Save_Success_Message"));
    }

    [RelayCommand]
    private void Close()
    {
        metadataModalWindow.DialogResult = false;
    }
}
