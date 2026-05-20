namespace MinimalTextEditorLite.Core.Models;

public static class AppThemePreference
{
    public const string Light = "light";
    public const string Dark = "dark";
    public const string System = "system";

    public static bool IsValid(string? value)
    {
        return string.Equals(value, Light, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, Dark, StringComparison.OrdinalIgnoreCase) ||
               string.Equals(value, System, StringComparison.OrdinalIgnoreCase);
    }

    public static string Normalize(string? value)
    {
        if (string.Equals(value, Dark, StringComparison.OrdinalIgnoreCase))
            return Dark;

        if (string.Equals(value, System, StringComparison.OrdinalIgnoreCase))
            return System;

        return Light;
    }
}
