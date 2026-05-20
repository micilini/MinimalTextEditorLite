namespace MinimalTextEditorLite.Core.Services;

public sealed record ExporterDescriptor(
    string Id,
    string DisplayName,
    string DefaultFileName,
    string FileDialogFilter
);
