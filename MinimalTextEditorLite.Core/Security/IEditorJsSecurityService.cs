namespace MinimalTextEditorLite.Core.Security;

public interface IEditorJsSecurityService
{
    Task<EditorJsSecurityResult> ValidateAndNormalizeJsonAsync(string json);
}

public sealed class EditorJsSecurityResult
{
    public bool Success { get; init; }

    public string? NormalizedJson { get; init; }

    public string? ErrorMessage { get; init; }

    public static EditorJsSecurityResult Ok(string normalizedJson) => new()
    {
        Success = true,
        NormalizedJson = normalizedJson
    };

    public static EditorJsSecurityResult Fail(string errorMessage) => new()
    {
        Success = false,
        ErrorMessage = errorMessage
    };
}
