using MinimalTextEditorLite.Exporters.Contracts.EditorJs;
using NJsonSchema;
using System.Reflection;
using System.Text.Json;

namespace MinimalTextEditorLite.Core.Security;

public sealed class EditorJsSecurityService(
    EditorJsImageValidator imageValidator,
    EditorJsInlineHtmlSanitizer htmlSanitizer) : IEditorJsSecurityService
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
            "header" => NormalizeHeaderBlock(block),
            "paragraph" => NormalizeParagraphBlock(block),
            "list" => NormalizeListBlock(block),
            "checklist" => NormalizeChecklistBlock(block),
            "quote" => NormalizeQuoteBlock(block),
            "warning" => NormalizeWarningBlock(block),
            "code" => NormalizeCodeBlock(block),
            "delimiter" => new EditorJsBlock { Id = block.Id, Type = block.Type, Data = block.Data },
            "table" => NormalizeTableBlock(block),
            "image" => NormalizeImageBlock(block),
            _ => null
        };
    }

    private EditorJsBlock NormalizeHeaderBlock(EditorJsBlock block)
    {
        try
        {
            var data = block.Data.Deserialize<EditorJsHeaderData>(EditorJsJson.Options);
            if (data == null || data.Text == null || data.Level is < 1 or > 6)
                return CreateInvalidBlockPlaceholder(block.Id);

            data.Text = htmlSanitizer.SanitizeInlineHtml(data.Text);
            return CreateBlock(block.Id, "header", data);
        }
        catch (JsonException)
        {
            return CreateInvalidBlockPlaceholder(block.Id);
        }
    }

    private EditorJsBlock NormalizeParagraphBlock(EditorJsBlock block)
    {
        try
        {
            var data = block.Data.Deserialize<EditorJsParagraphData>(EditorJsJson.Options);
            if (data == null || data.Text == null)
                return CreateInvalidBlockPlaceholder(block.Id);

            data.Text = htmlSanitizer.SanitizeInlineHtml(data.Text);
            return CreateBlock(block.Id, "paragraph", data);
        }
        catch (JsonException)
        {
            return CreateInvalidBlockPlaceholder(block.Id);
        }
    }

    private EditorJsBlock NormalizeListBlock(EditorJsBlock block)
    {
        try
        {
            var data = block.Data.Deserialize<EditorJsListData>(EditorJsJson.Options);
            if (data?.Items == null)
                return CreateInvalidBlockPlaceholder(block.Id);

            data.Items = data.Items
                .Select(item => htmlSanitizer.SanitizeInlineHtml(item))
                .ToList();

            return CreateBlock(block.Id, "list", data);
        }
        catch (JsonException)
        {
            return CreateInvalidBlockPlaceholder(block.Id);
        }
    }

    private EditorJsBlock NormalizeChecklistBlock(EditorJsBlock block)
    {
        try
        {
            var data = block.Data.Deserialize<EditorJsChecklistData>(EditorJsJson.Options);
            if (data?.Items == null)
                return CreateInvalidBlockPlaceholder(block.Id);

            foreach (var item in data.Items)
                item.Text = htmlSanitizer.SanitizeInlineHtml(item.Text);

            return CreateBlock(block.Id, "checklist", data);
        }
        catch (JsonException)
        {
            return CreateInvalidBlockPlaceholder(block.Id);
        }
    }

    private EditorJsBlock NormalizeQuoteBlock(EditorJsBlock block)
    {
        try
        {
            var data = block.Data.Deserialize<EditorJsQuoteData>(EditorJsJson.Options);
            if (data == null || (data.Text == null && data.Caption == null))
                return CreateInvalidBlockPlaceholder(block.Id);

            data.Text = htmlSanitizer.SanitizeInlineHtml(data.Text);
            data.Caption = htmlSanitizer.SanitizeInlineHtml(data.Caption);
            return CreateBlock(block.Id, "quote", data);
        }
        catch (JsonException)
        {
            return CreateInvalidBlockPlaceholder(block.Id);
        }
    }

    private EditorJsBlock NormalizeWarningBlock(EditorJsBlock block)
    {
        try
        {
            var data = block.Data.Deserialize<EditorJsWarningData>(EditorJsJson.Options);
            if (data == null || (data.Title == null && data.Message == null))
                return CreateInvalidBlockPlaceholder(block.Id);

            data.Title = htmlSanitizer.SanitizeInlineHtml(data.Title);
            data.Message = htmlSanitizer.SanitizeInlineHtml(data.Message);
            return CreateBlock(block.Id, "warning", data);
        }
        catch (JsonException)
        {
            return CreateInvalidBlockPlaceholder(block.Id);
        }
    }

    private EditorJsBlock NormalizeCodeBlock(EditorJsBlock block)
    {
        try
        {
            var data = block.Data.Deserialize<EditorJsCodeData>(EditorJsJson.Options);
            if (data == null || data.Code == null)
                return CreateInvalidBlockPlaceholder(block.Id);

            data.Code = htmlSanitizer.SanitizePlainText(data.Code);
            return CreateBlock(block.Id, "code", data);
        }
        catch (JsonException)
        {
            return CreateInvalidBlockPlaceholder(block.Id);
        }
    }

    private EditorJsBlock NormalizeTableBlock(EditorJsBlock block)
    {
        try
        {
            var data = block.Data.Deserialize<EditorJsTableData>(EditorJsJson.Options);
            if (data?.Content == null)
                return CreateInvalidBlockPlaceholder(block.Id);

            data.Content = data.Content
                .Select(row => row.Select(cell => htmlSanitizer.SanitizeInlineHtml(cell)).ToList())
                .ToList();

            return CreateBlock(block.Id, "table", data);
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
            if (data == null)
                return CreateInvalidBlockPlaceholder(block.Id);

            if (!imageValidator.IsValidImageUrl(data.Url, out _))
                return CreateInvalidImagePlaceholder(block.Id, data.Caption, data.Url);

            data.Caption = htmlSanitizer.SanitizeInlineHtml(data.Caption);
            data.FileName = htmlSanitizer.SanitizePlainText(data.FileName);
            data.MimeType = htmlSanitizer.SanitizePlainText(data.MimeType);

            if (data.Width <= 0)
                data.Width = null;

            if (data.Height <= 0)
                data.Height = null;

            if (data.Size <= 0)
                data.Size = null;

            return CreateBlock(block.Id, "image", data);
        }
        catch (JsonException)
        {
            return CreateInvalidBlockPlaceholder(block.Id);
        }
    }

    private static EditorJsBlock CreateBlock<T>(string? id, string type, T data)
    {
        return new EditorJsBlock
        {
            Id = id,
            Type = type,
            Data = JsonSerializer.SerializeToElement(data, EditorJsJson.Options)
        };
    }

    private static EditorJsBlock CreateInvalidBlockPlaceholder(string? id)
    {
        return CreateBlock(
            id,
            "paragraph",
            new EditorJsParagraphData { Text = "[Invalid block removed]" });
    }

    private EditorJsBlock CreateInvalidImagePlaceholder(string? id, string? caption, string? url)
    {
        var text = string.IsNullOrWhiteSpace(caption)
            ? "[Image removed: unsupported image source]"
            : $"[Image removed: {htmlSanitizer.SanitizePlainText(caption)}]";

        return CreateBlock(
            id,
            "paragraph",
            new EditorJsParagraphData { Text = text });
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
