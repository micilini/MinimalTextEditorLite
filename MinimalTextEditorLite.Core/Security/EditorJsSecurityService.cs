using MinimalTextEditorLite.Exporters.Contracts.EditorJs;
using NJsonSchema;
using System.Reflection;
using System.Text.Json;

namespace MinimalTextEditorLite.Core.Security;

public sealed class EditorJsSecurityService(EditorJsImageValidator imageValidator) : IEditorJsSecurityService
{
    private readonly Lazy<Task<JsonSchema>> schema = new(LoadSchemaAsync);

    public async Task<EditorJsSecurityResult> ValidateAndNormalizeJsonAsync(string json)
    {
        if (string.IsNullOrWhiteSpace(json))
            return EditorJsSecurityResult.Fail("EditorJS JSON is empty.");

        try
        {
            using var document = JsonDocument.Parse(json);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
                return EditorJsSecurityResult.Fail("EditorJS JSON root must be an object.");

            var validationErrors = (await schema.Value).Validate(json);
            if (validationErrors.Count > 0)
                return EditorJsSecurityResult.Fail("EditorJS JSON does not match the expected schema.");

            var editorDocument = JsonSerializer.Deserialize<EditorJsDocument>(json, EditorJsJson.Options);
            if (editorDocument == null)
                return EditorJsSecurityResult.Fail("EditorJS JSON could not be read.");

            editorDocument.Blocks = NormalizeBlocks(editorDocument.Blocks);

            return EditorJsSecurityResult.Ok(JsonSerializer.Serialize(editorDocument, EditorJsJson.Options));
        }
        catch (JsonException ex)
        {
            return EditorJsSecurityResult.Fail(ex.Message);
        }
    }

    private List<EditorJsBlock> NormalizeBlocks(List<EditorJsBlock>? blocks)
    {
        var normalized = new List<EditorJsBlock>();

        foreach (var block in blocks ?? [])
        {
            if (block.Data.ValueKind != JsonValueKind.Object)
            {
                normalized.Add(CreateInvalidBlockPlaceholder(block.Id));
                continue;
            }

            var normalizedBlock = NormalizeBlock(block);
            if (normalizedBlock != null)
                normalized.Add(normalizedBlock);
        }

        return normalized;
    }

    private EditorJsBlock? NormalizeBlock(EditorJsBlock block)
    {
        return block.Type switch
        {
            "header" => NormalizeKnownBlock<EditorJsHeaderData>(block, IsValidHeaderData),
            "paragraph" => NormalizeKnownBlock<EditorJsParagraphData>(block, data => data.Text != null),
            "list" => NormalizeKnownBlock<EditorJsListData>(block, data => data.Items != null),
            "checklist" => NormalizeKnownBlock<EditorJsChecklistData>(block, data => data.Items != null),
            "quote" => NormalizeKnownBlock<EditorJsQuoteData>(block, data => data.Text != null || data.Caption != null),
            "warning" => NormalizeKnownBlock<EditorJsWarningData>(block, data => data.Title != null || data.Message != null),
            "code" => NormalizeKnownBlock<EditorJsCodeData>(block, data => data.Code != null),
            "delimiter" => new EditorJsBlock { Id = block.Id, Type = block.Type, Data = block.Data },
            "table" => NormalizeKnownBlock<EditorJsTableData>(block, data => data.Content != null),
            "image" => NormalizeImageBlock(block),
            _ => null
        };
    }

    private EditorJsBlock NormalizeKnownBlock<T>(EditorJsBlock block, Func<T, bool> isValid)
    {
        try
        {
            var data = block.Data.Deserialize<T>(EditorJsJson.Options);
            if (data == null || !isValid(data))
                return CreateInvalidBlockPlaceholder(block.Id);

            return new EditorJsBlock
            {
                Id = block.Id,
                Type = block.Type,
                Data = JsonSerializer.SerializeToElement(data, EditorJsJson.Options)
            };
        }
        catch (JsonException)
        {
            return CreateInvalidBlockPlaceholder(block.Id);
        }
    }

    private EditorJsBlock NormalizeImageBlock(EditorJsBlock block)
    {
        try
        {
            var data = block.Data.Deserialize<EditorJsImageData>(EditorJsJson.Options);
            if (data == null || !imageValidator.IsValidImageUrl(data.Url, out _))
                return CreateInvalidBlockPlaceholder(block.Id);

            return new EditorJsBlock
            {
                Id = block.Id,
                Type = block.Type,
                Data = JsonSerializer.SerializeToElement(data, EditorJsJson.Options)
            };
        }
        catch (JsonException)
        {
            return CreateInvalidBlockPlaceholder(block.Id);
        }
    }

    private static bool IsValidHeaderData(EditorJsHeaderData data)
    {
        return data.Text != null && data.Level is >= 1 and <= 6;
    }

    private static EditorJsBlock CreateInvalidBlockPlaceholder(string? id)
    {
        return new EditorJsBlock
        {
            Id = id,
            Type = "paragraph",
            Data = JsonSerializer.SerializeToElement(
                new EditorJsParagraphData { Text = "[Invalid block removed]" },
                EditorJsJson.Options)
        };
    }

    private static async Task<JsonSchema> LoadSchemaAsync()
    {
        var assembly = Assembly.GetExecutingAssembly();
        var resourceName = assembly
            .GetManifestResourceNames()
            .FirstOrDefault(name => name.EndsWith("Schemas.editorjs-schema.json", StringComparison.OrdinalIgnoreCase));

        if (resourceName == null)
            throw new InvalidOperationException("EditorJS schema embedded resource was not found.");

        await using var stream = assembly.GetManifestResourceStream(resourceName)
            ?? throw new InvalidOperationException("EditorJS schema embedded resource could not be opened.");

        using var reader = new StreamReader(stream);
        var schemaJson = await reader.ReadToEndAsync();

        return await JsonSchema.FromJsonAsync(schemaJson);
    }
}
