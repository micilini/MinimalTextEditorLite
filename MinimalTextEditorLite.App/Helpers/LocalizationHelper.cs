using System.IO;
using System.Text.Json;
using System.Windows;

namespace MinimalTextEditorLite.App.Helpers;

public class LocalizationHelper
{
    public static string LocalizationFile = "localization.config";

    public static void CreateLocalizationFileIfNeeded(string defaultLanguage)
    {
        string localizationConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MinimalTextEditorLite",
            LocalizationFile);

        if (File.Exists(localizationConfigPath))
        {
            string content = File.ReadAllText(localizationConfigPath).Trim();

            if (content == "en_us" || content == "pt_br")
                ((App)Application.Current).AppLanguage = content;
        }
        else
        {
            File.WriteAllText(localizationConfigPath, defaultLanguage);
            ((App)Application.Current).AppLanguage = defaultLanguage;
        }
    }

    public static void UpdateLocalizationFile(string newLanguage)
    {
        string localizationConfigPath = Path.Combine(
            Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
            "MinimalTextEditorLite",
            LocalizationFile);

        File.WriteAllText(localizationConfigPath, newLanguage);
        ((App)Application.Current).AppLanguage = newLanguage;
    }

    private readonly Dictionary<string, string> translations;

    public LocalizationHelper(string languageCode)
    {
        var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages", $"{languageCode}.json");

        if (!File.Exists(filePath))
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Language_Not_Found"));
            Application.Current.MainWindow.Close();
        }

        var jsonContent = File.ReadAllText(filePath);
        translations = JsonSerializer.Deserialize<Dictionary<string, string>>(jsonContent)
                       ?? new Dictionary<string, string>();
    }

    public string Translate(string key)
    {
        return translations.TryGetValue(key, out var value) ? value : $"[{key}]";
    }
}


