﻿using Newtonsoft.Json.Linq;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using System.Windows;

namespace Minimal_Text_Editor__Lite_.ViewModel.Helpers
{
    public class UpdatesCheck
    {
        public async void CheckForUpdates()
        {
            try
            {
                // Selecionando as propriedades do App.xaml.cs
                string applicationVersion = ((App)Application.Current).ApplicationVersion;
                string applicationIdentifier = ((App)Application.Current).ApplicationIdentifier;
                bool showNewUpdates = ((App)Application.Current).ShowNewUpdates;

                // Montando a URL para a chamada GET
                //string url = $"http://localhost/mte/updates.php?version={applicationVersion}&identifier={applicationIdentifier}";
                string url = $"https://micilini.com/apps/mte-lite/updates?version={applicationVersion}&identifier={applicationIdentifier}";

                // Fazendo a chamada HTTP GET
                using (HttpClient client = new HttpClient())
                {
                    HttpResponseMessage response = await client.GetAsync(url);
                    response.EnsureSuccessStatusCode();
                    string responseBody = await response.Content.ReadAsStringAsync();

                    // Processando o JSON de resposta
                    var json = JObject.Parse(responseBody);
                    bool newVersion = json["newVersion"].Value<bool>();
                    string version = json["Version"].Value<string>();
                    string urlUpdate = json["URLUpdate"].Value<string>();

                    // Exibindo o MessageBox apenas se ShowNewUpdates for TRUE
                    if (showNewUpdates && newVersion)
                    {
                        ShowUpdateMessageBox(version, urlUpdate);
                    }
                }
            }
            catch (Exception ex)
            {
                //Tratamento de Erros para Atualizações
            }
        }

        private void ShowUpdateMessageBox(string version, string urlUpdate)
        {
            // Configurando os botões do MessageBox
            MessageBoxResult result = MessageBox.Show(
                $"Uma nova versão ({version}) está disponível! Deseja fazer o download?",
                "Atualização Disponível",
                MessageBoxButton.YesNo,
                MessageBoxImage.Information
            );

            // Ações baseadas no botão clicado
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
}
