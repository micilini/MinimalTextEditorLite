using MinimalTextEditorLite.Core.Repositories;
using MinimalTextEditorLite.Core.Security;

namespace MinimalTextEditorLite.Core.Services;

public sealed class ImportService(
    INoteRepository noteRepository,
    IRecentFilesRepository recentFilesRepository,
    IEditorJsSecurityService editorJsSecurityService) : IImportService
{
    public async Task<ImportResult> ImportAsync(string filePath)
    {
        if (!File.Exists(filePath))
            return ImportResult.Fail("Error_Note_Import");

        if (!string.Equals(Path.GetExtension(filePath), ".json", StringComparison.OrdinalIgnoreCase))
            return ImportResult.Fail("Error_Invalid_Json");

        string jsonContent;
        try
        {
            jsonContent = await File.ReadAllTextAsync(filePath);
        }
        catch (Exception ex)
        {
            return ImportResult.Fail("Error_Note_Import", ex.Message);
        }

        var validation = await editorJsSecurityService.ValidateAndNormalizeJsonAsync(jsonContent);
        if (!validation.Success || string.IsNullOrWhiteSpace(validation.NormalizedJson))
            return ImportResult.Fail("Error_Invalid_Json", validation.ErrorMessage);

        var updated = await noteRepository.UpdateJsonAsync(validation.NormalizedJson);
        if (!updated)
            return ImportResult.Fail("Error_Notes_Not_Found");

        await recentFilesRepository.RegisterAsync(filePath);

        return ImportResult.Ok();
    }
}
