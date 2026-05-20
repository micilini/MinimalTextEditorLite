namespace MinimalTextEditorLite.Core.Services;

public sealed class ImportResult
{
    public bool Success { get; init; }
    public string? ErrorKey { get; init; }
    public string? ErrorMessage { get; init; }

    public static ImportResult Ok() => new() { Success = true };

    public static ImportResult Fail(string errorKey, string? errorMessage = null) =>
        new() { Success = false, ErrorKey = errorKey, ErrorMessage = errorMessage };
}
