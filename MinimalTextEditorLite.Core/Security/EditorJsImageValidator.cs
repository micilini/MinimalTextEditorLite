namespace MinimalTextEditorLite.Core.Security;

public sealed class EditorJsImageValidator
{
    public const long MaxImageBytes = 50L * 1024 * 1024;

    private static readonly HashSet<string> AllowedDataImageTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/gif",
        "image/webp",
        "image/svg+xml"
    };

    public bool IsValidImageUrl(string? url, out string reason)
    {
        reason = string.Empty;

        if (string.IsNullOrWhiteSpace(url))
        {
            reason = "Image URL is empty.";
            return false;
        }

        url = url.Trim();

        if (url.StartsWith("data:", StringComparison.OrdinalIgnoreCase))
            return IsValidDataImage(url, out reason);

        if (!Uri.TryCreate(url, UriKind.Absolute, out var uri))
        {
            reason = "Image URL is invalid.";
            return false;
        }

        if (uri.Scheme.Equals(Uri.UriSchemeHttps, StringComparison.OrdinalIgnoreCase) ||
            uri.Scheme.Equals(Uri.UriSchemeHttp, StringComparison.OrdinalIgnoreCase))
        {
            return true;
        }

        reason = $"Image scheme '{uri.Scheme}' is not allowed.";
        return false;
    }

    private static bool IsValidDataImage(string url, out string reason)
    {
        reason = string.Empty;

        var commaIndex = url.IndexOf(',');
        if (commaIndex < 0)
        {
            reason = "Data image is malformed.";
            return false;
        }

        var metadata = url[..commaIndex];
        var base64 = url[(commaIndex + 1)..];
        var metadataParts = metadata.Split(';', StringSplitOptions.RemoveEmptyEntries);

        if (metadataParts.Length < 2 || !metadataParts[0].StartsWith("data:", StringComparison.OrdinalIgnoreCase))
        {
            reason = "Data image metadata is malformed.";
            return false;
        }

        var mediaType = metadataParts[0]["data:".Length..];
        if (!AllowedDataImageTypes.Contains(mediaType))
        {
            reason = "Data image media type is not allowed.";
            return false;
        }

        if (!metadataParts.Any(part => part.Equals("base64", StringComparison.OrdinalIgnoreCase)))
        {
            reason = "Data image must be base64 encoded.";
            return false;
        }

        var estimatedBytes = (long)base64.Length * 3 / 4;
        if (estimatedBytes > MaxImageBytes)
        {
            reason = "Image too large.";
            return false;
        }

        try
        {
            Convert.FromBase64String(base64);
        }
        catch (FormatException)
        {
            reason = "Data image base64 is invalid.";
            return false;
        }

        return true;
    }
}
