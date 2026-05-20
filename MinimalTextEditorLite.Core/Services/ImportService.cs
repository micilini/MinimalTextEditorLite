using MinimalTextEditorLite.Core.Repositories;
using System.Text.Json;

namespace MinimalTextEditorLite.Core.Services;

public sealed class ImportService(INoteRepository noteRepository, IRecentFilesRepository recentFilesRepository) : IImportService
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

        try
        {
            using var jsonObject = JsonDocument.Parse(jsonContent);
            if (!jsonObject.RootElement.TryGetProperty("blocks", out _))
                return ImportResult.Fail("Error_Invalid_Json_Key_Block");
        }
        catch (JsonException ex)
        {
            return ImportResult.Fail("Error_Invalid_Json", ex.Message);
        }

        var updated = await noteRepository.UpdateJsonAsync(jsonContent);
        if (!updated)
            return ImportResult.Fail("Error_Notes_Not_Found");

        await recentFilesRepository.RegisterAsync(filePath);

        return ImportResult.Ok();
    }
}
