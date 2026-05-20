using System.Net.Http;
using System.Text.Json;
using System.Windows;

namespace MinimalTextEditorLite.App.Helpers;

public class UpdatesCheck
{
    public async Task CheckForUpdates(bool showNoUpdatedAvailableMessage = true)
    {
        try
        {
            string applicationVersion = ((App)Application.Current).ApplicationVersion;
            string applicationIdentifier = ((App)Application.Current).ApplicationIdentifier;
            bool showNewUpdates = ((App)Application.Current).ShowNewUpdates;
            string url = $"https://micilini.com/apps/mte-lite/updates?version={applicationVersion}&identifier={applicationIdentifier}";

            using HttpClient client = new HttpClient();
            HttpResponseMessage response = await client.GetAsync(url);
            response.EnsureSuccessStatusCode();
            string responseBody = await response.Content.ReadAsStringAsync();

            using var json = JsonDocument.Parse(responseBody);
            var root = json.RootElement;

            bool newVersion = root.TryGetProperty("newVersion", out var newVersionElement)
                              && newVersionElement.GetBoolean();

            string version = root.TryGetProperty("Version", out var versionElement)
                ? versionElement.GetString() ?? string.Empty
                : string.Empty;

            string urlUpdate = root.TryGetProperty("URLUpdate", out var urlElement)
                ? urlElement.GetString() ?? string.Empty
                : string.Empty;

            if (showNewUpdates && newVersion)
            {
                ShowUpdateMessageBox(version, urlUpdate);
                return;
            }

            if (!showNoUpdatedAvailableMessage)
            {
                MessageBox.Show(App.Localization.Translate("Updates_Description_Fail"),
                    App.Localization.Translate("Updates_Title_Fail"), MessageBoxButton.OK, MessageBoxImage.Information);
            }
        }
        catch
        {
        }
    }

    private void ShowUpdateMessageBox(string version, string urlUpdate)
    {
        var template = App.Localization.Translate("Updates_Description_OK");
        var message = string.Format(template, version);

        MessageBoxResult result = MessageBox.Show(
            message,
            App.Localization.Translate("Updates_Title_OK"),
            MessageBoxButton.YesNo,
            MessageBoxImage.Information);

        if (result == MessageBoxResult.Yes)
        {
            System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
            {
                FileName = urlUpdate,
                UseShellExecute = true
            });
        }
    }
}
