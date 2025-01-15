using Newtonsoft.Json;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection.Metadata;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Minimal_Text_Editor__Lite_.ViewModel.Helpers
{
    public class LocalizationHelper
    {
        //Métodos e Propriedades Estáticas
        public static string LocalizationFile = "localization.config";

        public static void CreateLocalizationFileIfNedded(string defaultLanguage)
        {
            string localizationConfigPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "MinimalTextEditorLite",
                LocalizationFile
            );

            if (File.Exists(localizationConfigPath))
            {
                string content = File.ReadAllText(localizationConfigPath).Trim();

                if (content == "en_us" || content == "pt_br")
                {
                    ((App)Application.Current).AppLanguage = content;
                }
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
                LocalizationFile
            );

            if (File.Exists(localizationConfigPath))
            {
                File.WriteAllText(localizationConfigPath, newLanguage);
            }
            else
            {
                File.WriteAllText(localizationConfigPath, newLanguage);
            }

            ((App)Application.Current).AppLanguage = newLanguage;
        }

        //Métodos e Propriedades da Classe
        private readonly Dictionary<string, string> _translations;

        public LocalizationHelper(string languageCode)
        {
            var filePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Languages", $"{languageCode}.json");

            if (!File.Exists(filePath))
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Language_Not_Found"));
                Application.Current.MainWindow.Close();
            }

            var jsonContent = File.ReadAllText(filePath);
            _translations = JsonConvert.DeserializeObject<Dictionary<string, string>>(jsonContent)
                            ?? new Dictionary<string, string>();
        }

        public string Translate(string key)
        {
            return _translations.TryGetValue(key, out var value) ? value : $"[{key}]";
        }
    }
}
