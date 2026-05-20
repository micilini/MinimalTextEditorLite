namespace MinimalTextEditorLite.App.Services;

public interface IThemeService
{
    void Apply(string? themePreference);
    string ResolveEffectiveTheme(string? themePreference);
}
