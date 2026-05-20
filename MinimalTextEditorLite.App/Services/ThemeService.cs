using MaterialDesignThemes.Wpf;
using Microsoft.Win32;
using MinimalTextEditorLite.Core.Models;
using System.Windows;
using MdTheme = MaterialDesignThemes.Wpf.Theme;

namespace MinimalTextEditorLite.App.Services;

public sealed class ThemeService : IThemeService
{
    public void Apply(string? themePreference)
    {
        var normalizedPreference = AppThemePreference.Normalize(themePreference);
        var effectiveTheme = ResolveEffectiveTheme(normalizedPreference);

        ApplyMaterialDesignTheme(effectiveTheme);

        if (Application.Current is App app)
        {
            app.ThemePreference = normalizedPreference;
            app.EffectiveTheme = effectiveTheme;
        }
    }

    public string ResolveEffectiveTheme(string? themePreference)
    {
        var normalizedPreference = AppThemePreference.Normalize(themePreference);

        if (normalizedPreference == AppThemePreference.System)
            return IsWindowsDarkModeEnabled() ? AppThemePreference.Dark : AppThemePreference.Light;

        return normalizedPreference;
    }

    private static void ApplyMaterialDesignTheme(string effectiveTheme)
    {
        var paletteHelper = new PaletteHelper();
        var theme = paletteHelper.GetTheme();

        theme.SetBaseTheme(effectiveTheme == AppThemePreference.Dark
            ? MdTheme.Dark
            : MdTheme.Light);

        paletteHelper.SetTheme(theme);
    }

    private static bool IsWindowsDarkModeEnabled()
    {
        try
        {
            using var key = Registry.CurrentUser.OpenSubKey(
                @"Software\Microsoft\Windows\CurrentVersion\Themes\Personalize");

            var value = key?.GetValue("AppsUseLightTheme");

            return value is int intValue && intValue == 0;
        }
        catch
        {
            return false;
        }
    }
}
