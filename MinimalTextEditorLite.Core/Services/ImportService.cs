using MinimalTextEditorLite.Core.Importers;
using MinimalTextEditorLite.Core.Repositories;
using MinimalTextEditorLite.Core.Security;

namespace MinimalTextEditorLite.Core.Services;

public sealed class ImportService : IImportService
{
    private readonly INoteRepository noteRepository;
    private readonly IRecentFilesRepository recentFilesRepository;
    private readonly IEditorJsSecurityService editorJsSecurityService;
    private readonly IReadOnlyDictionary<string, IImporter> importersByExtension;

    public ImportService(
        INoteRepository noteRepository,
        IRecentFilesRepository recentFilesRepository,
        IEditorJsSecurityService editorJsSecurityService,
        IEnumerable<IImporter> importers)
    {
        this.noteRepository = noteRepository;
        this.recentFilesRepository = recentFilesRepository;
        this.editorJsSecurityService = editorJsSecurityService;
        importersByExtension = importers.ToDictionary(importer => importer.Extension, StringComparer.OrdinalIgnoreCase);
    }

    public async Task<ImportResult> ImportAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return ImportResult.Fail("Error_Note_Import");

        var extension = Path.GetExtension(filePath);

        if (!importersByExtension.TryGetValue(extension, out var importer))
            return ImportResult.Fail("Error_Unsupported_File_Format");

        string fileContent;
        try
        {
            fileContent = await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            return ImportResult.Fail("Error_Note_Import", ex.Message);
        }

        ImportedNoteContent imported;
        try
        {
            imported = await importer.ImportAsync(fileContent, filePath);
        }
        catch (Exception ex)
        {
            return ImportResult.Fail(GetInvalidFileErrorKey(extension), ex.Message);
        }

        var validation = await editorJsSecurityService.ValidateAndNormalizeJsonAsync(imported.EditorJsJson);
        if (!validation.Success || string.IsNullOrWhiteSpace(validation.NormalizedJson))
            return ImportResult.Fail(GetInvalidFileErrorKey(extension), validation.ErrorMessage);

        var updated = imported.Metadata?.HasAnyValue == true
            ? await noteRepository.UpdateJsonAndMetadataAsync(validation.NormalizedJson, imported.Metadata)
            : await noteRepository.UpdateJsonAsync(validation.NormalizedJson);
        if (!updated)
            return ImportResult.Fail("Error_Notes_Not_Found");

        await recentFilesRepository.RegisterAsync(filePath);

        return ImportResult.Ok();
    }

    private static string GetInvalidFileErrorKey(string extension)
    {
        return string.Equals(extension, ".md", StringComparison.OrdinalIgnoreCase)
            ? "Error_Invalid_Markdown"
            : "Error_Invalid_Json";
    }
}
