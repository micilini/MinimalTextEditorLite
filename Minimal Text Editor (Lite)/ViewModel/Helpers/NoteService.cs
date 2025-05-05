
using Microsoft.Win32;
using Minimal_Text_Editor__Lite_.Model;
using Newtonsoft.Json;
using System.IO;
using System.Windows;
using System.Text;
using System.Diagnostics;


namespace Minimal_Text_Editor__Lite_.ViewModel.Helpers
{
    public class NoteService
    {
        public static async Task<bool> ExportCurrentNoteAsJSON()
        {
            try
            {
                // 1. Buscar a nota com Id igual a 1 no banco de dados
                var note = DatabaseHelper.QuerySingle<NoteModel>(
                    "SELECT * FROM Note WHERE Id = ?", 1);

                if (note == null)
                {
                    ModalMessages.showErrorModal(App.Localization.Translate("Error_Notes_Not_Found"));
                    return false;
                }

                // 2. Abrir a caixa de diálogo para salvar o arquivo
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    DefaultExt = "json",
                    FileName = "Note.json",
                    Title = App.Localization.Translate("Title_Export_Note")
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // 3. Salvar o conteúdo no arquivo selecionado
                    await File.WriteAllTextAsync(saveFileDialog.FileName, note.NoteJson);
                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ModalMessages.showErrorModal($"{App.Localization.Translate("Error_Note_Export")} - {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> ExportCurrentNoteAsDoc()
        {
            try
            {
                // 1. Buscar a nota no banco de dados
                var note = DatabaseHelper.QuerySingle<NoteModel>(
                    "SELECT * FROM Note WHERE Id = ?", 1);

                if (note == null)
                {
                    ModalMessages.showErrorModal(App.Localization.Translate("Error_Notes_Not_Found"));
                    return false; // Impede a continuação do fluxo
                }

                // 2. Criar um arquivo temporário para armazenar o JSON
                string tempJsonFilePath = Path.GetTempFileName();
                await File.WriteAllTextAsync(tempJsonFilePath, note.NoteJson);

                // 3. Executar o processo de forma assíncrona
                var result = await Task.Run(() =>
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = "Modules\\Export\\x64\\ExportAsDOC.exe",
                        Arguments = $"\"{tempJsonFilePath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Path.GetTempPath()
                    };

                    using (var process = new Process { StartInfo = processStartInfo })
                    {
                        process.Start();

                        using (var memoryStream = new MemoryStream())
                        {
                            process.StandardOutput.BaseStream.CopyTo(memoryStream);
                            process.WaitForExit();

                            if (process.ExitCode != 0)
                            {
                                var error = process.StandardError.ReadToEnd();
                                throw new Exception(error);
                            }

                            return memoryStream.ToArray(); // Retorna o binário do arquivo DOC
                        }
                    }
                });

                // 4. Abrir caixa de diálogo para salvar o arquivo
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Word Documents (*.docx)|*.docx",
                    DefaultExt = "docx",
                    FileName = "Note.docx",
                    Title = App.Localization.Translate("Title_Export_Note")
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await File.WriteAllBytesAsync(saveFileDialog.FileName, result);
                    
                    // 5. Limpar o arquivo temporário
                    File.Delete(tempJsonFilePath);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ModalMessages.showErrorModal($"{App.Localization.Translate("Error_Note_Export")} - {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> ExportCurrentNoteAsPDF()
        {
            try
            {
                // 1. Buscar a nota no banco de dados
                var note = DatabaseHelper.QuerySingle<NoteModel>(
                    "SELECT * FROM Note WHERE Id = ?", 1);

                if (note == null)
                {
                    ModalMessages.showErrorModal(App.Localization.Translate("Error_Notes_Not_Found"));
                    return false; // Impede a continuação do fluxo
                }

                // 2. Criar um arquivo temporário para armazenar o JSON
                string tempJsonFilePath = Path.GetTempFileName();
                await File.WriteAllTextAsync(tempJsonFilePath, note.NoteJson);

                // 3. Executar o processo de forma assíncrona
                var result = await Task.Run(() =>
                {
                    var processStartInfo = new ProcessStartInfo
                    {
                        FileName = "Modules\\Export\\x64\\ExportAsPDF.exe",
                        Arguments = $"\"{tempJsonFilePath}\"",
                        RedirectStandardOutput = true,
                        RedirectStandardError = true,
                        UseShellExecute = false,
                        CreateNoWindow = true,
                        WorkingDirectory = Path.GetTempPath()
                    };

                    using (var process = new Process { StartInfo = processStartInfo })
                    {
                        process.Start();

                        using (var memoryStream = new MemoryStream())
                        {
                            process.StandardOutput.BaseStream.CopyTo(memoryStream);
                            process.WaitForExit();

                            if (process.ExitCode != 0)
                            {
                                var error = process.StandardError.ReadToEnd();
                                throw new Exception(error);
                            }

                            return memoryStream.ToArray(); // Retorna o binário do arquivo DOC
                        }
                    }
                });

                // 4. Abrir caixa de diálogo para salvar o arquivo
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "Word Documents (*.pdf)|*.pdf",
                    DefaultExt = "pdf",
                    FileName = "Note.pdf",
                    Title = App.Localization.Translate("Title_Export_Note")
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    await File.WriteAllBytesAsync(saveFileDialog.FileName, result);

                    // 5. Limpar o arquivo temporário
                    File.Delete(tempJsonFilePath);

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ModalMessages.showErrorModal($"{App.Localization.Translate("Error_Note_Export")} - {ex.Message}");
                return false;
            }
        }

        public static async Task<bool> ExportCurrentNoteAsHTML()
        {
            try
            {
                // 1. Buscar a nota com Id igual a 1 no banco de dados
                var note = DatabaseHelper.QuerySingle<NoteModel>(
                    "SELECT * FROM Note WHERE Id = ?", 1);

                if (note == null)
                {
                    ModalMessages.showErrorModal(App.Localization.Translate("Error_Notes_Not_Found"));
                    return false;
                }

                // 2. Converter JSON da nota em objeto dinâmico
                dynamic noteJson = JsonConvert.DeserializeObject(note.NoteJson);

                // 3. Construir o conteúdo HTML
                var htmlBuilder = new StringBuilder();
                htmlBuilder.Append("<!DOCTYPE html>");
                htmlBuilder.Append("<html lang='en'>");
                htmlBuilder.Append("<head>");
                htmlBuilder.Append("<meta charset='UTF-8'>");
                htmlBuilder.Append("<meta name='viewport' content='width=device-width, initial-scale=1.0'>");
                htmlBuilder.Append("<title>Note Export</title>");
                htmlBuilder.Append("<style>");
                htmlBuilder.Append("body { font-family: Arial, sans-serif; line-height: 1.6; }");
                htmlBuilder.Append("h1, h2, h3, h4, h5, h6 { margin: 10px 0; }");
                htmlBuilder.Append("p { margin: 10px 0; }");
                htmlBuilder.Append("ul, ol { margin: 10px 0; padding-left: 20px; }");
                htmlBuilder.Append("blockquote { margin: 10px 0; padding: 10px 20px; background: #f9f9f9; border-left: 4px solid #ccc; }");
                htmlBuilder.Append("code { font-family: 'Courier New', monospace; background: #f4f4f4; padding: 2px 4px; border-radius: 4px; }");
                htmlBuilder.Append("table { border-collapse: collapse; width: 100%; margin: 10px 0; }");
                htmlBuilder.Append("th, td { border: 1px solid #ccc; padding: 8px; text-align: left; }");
                htmlBuilder.Append("img { max-width: 100%; height: auto; margin: 10px 0; }");
                htmlBuilder.Append("</style>");
                htmlBuilder.Append("</head>");
                htmlBuilder.Append("<body>");

                foreach (var block in noteJson.blocks)
                {
                    string type = block.type;
                    dynamic data = block.data;

                    switch (type)
                    {
                        case "header":
                            htmlBuilder.Append($"<h{data.level}>{data.text}</h{data.level}>");
                            break;

                        case "paragraph":
                            htmlBuilder.Append($"<p>{data.text}</p>");
                            break;

                        case "list":
                            string listTag = data.style == "ordered" ? "ol" : "ul";
                            htmlBuilder.Append($"<{listTag}>");
                            foreach (var item in data.items)
                            {
                                htmlBuilder.Append($"<li>{item}</li>");
                            }
                            htmlBuilder.Append($"</{listTag}>");
                            break;

                        case "checklist":
                            htmlBuilder.Append("<ul>");
                            foreach (var item in data.items)
                            {
                                string checkbox = (bool)item["checked"] ? "☑" : "☐";
                                htmlBuilder.Append($"{checkbox} {item.text}<br>");
                            }
                            htmlBuilder.Append("</ul>");
                            break;

                        case "quote":
                            htmlBuilder.Append($"<blockquote><p>{data.text}</p><footer>- {data.caption}</footer></blockquote>");
                            break;

                        case "warning":
                            htmlBuilder.Append($"<div style='border: 1px solid #ffa500; padding: 10px; margin: 10px 0; background: #fff8e5;'>");
                            htmlBuilder.Append($"<strong>{data.title}</strong>: {data.message}");
                            htmlBuilder.Append("</div>");
                            break;

                        case "code":
                            htmlBuilder.Append($"<pre><code>{System.Security.SecurityElement.Escape(data.code.ToString())}</code></pre>");
                            break;

                        case "delimiter":
                            htmlBuilder.Append("<p style='text-align:center; font-size:28px; margin:15px;'>***</p>");
                            break;

                        case "table":
                            htmlBuilder.Append("<table>");
                            foreach (var row in data.content)
                            {
                                htmlBuilder.Append("<tr>");
                                foreach (var cell in row)
                                {
                                    htmlBuilder.Append($"<td>{cell}</td>");
                                }
                                htmlBuilder.Append("</tr>");
                            }
                            htmlBuilder.Append("</table>");
                            break;

                        case "image":
                            htmlBuilder.Append($"<figure>");
                            htmlBuilder.Append($"<img src=\"{data.url}\" alt=\"{data.caption}\">");
                            if (!string.IsNullOrEmpty((string)data.caption))
                            {
                                htmlBuilder.Append($"<figcaption>{data.caption}</figcaption>");
                            }
                            htmlBuilder.Append("</figure>");
                            break;

                        default:
                            break; // Adicionado para evitar erro de queda para fora do switch
                    }
                }

                htmlBuilder.Append("</body>");
                htmlBuilder.Append("</html>");

                // 4. Abrir a caixa de diálogo para salvar o arquivo
                var saveFileDialog = new SaveFileDialog
                {
                    Filter = "HTML Files (*.html)|*.html",
                    DefaultExt = "html",
                    FileName = "Note.html",
                    Title = App.Localization.Translate("Title_Export_Note")
                };

                if (saveFileDialog.ShowDialog() == true)
                {
                    // 5. Salvar o conteúdo no arquivo selecionado
                    await File.WriteAllTextAsync(saveFileDialog.FileName, htmlBuilder.ToString());

                    return true;
                }

                return false;
            }
            catch (Exception ex)
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Note_Export"));
                return false;
            }
        }

        public static bool OpenNewNote()
        {
            try
            {
                // 1. Abrir a caixa de diálogo para selecionar o arquivo JSON
                var openFileDialog = new OpenFileDialog
                {
                    Filter = "JSON Files (*.json)|*.json",
                    DefaultExt = "json",
                    Title = App.Localization.Translate("Title_Open_Note")
                };

                // Se o usuário cancelar, retorna false imediatamente
                if (openFileDialog.ShowDialog() != true)
                    return false;


                // 2. Ler o conteúdo do arquivo selecionado
                string jsonContent = File.ReadAllText(openFileDialog.FileName);

                // 3. Validar se o JSON é válido
                if (!IsValidJson(jsonContent))
                {
                    ModalMessages.showErrorModal(App.Localization.Translate("Error_Invalid_Json"));
                    return false;
                }

                // 4. Validar se contém a chave 'blocks' na primeira ninhada
                var jsonObject = System.Text.Json.JsonDocument.Parse(jsonContent);
                if (!jsonObject.RootElement.TryGetProperty("blocks", out _))
                {
                   ModalMessages.showErrorModal(App.Localization.Translate("Error_Invalid_Json_Key_Block"));
                   return false;
                }

                // 5. Atualizar o banco de dados com o conteúdo do JSON
                var note = DatabaseHelper.QuerySingle<NoteModel>("SELECT * FROM Note WHERE Id = ?", 1);

                if (note != null)
                {
                    note.NoteJson = jsonContent;
                    note.UpdatedAt = DateTime.UtcNow;
                    DatabaseHelper.Update(note);

                    // Tudo ok, devolve true para carregar a nota na UI
                    return true;
                }
                else
                {
                    ModalMessages.showErrorModal(App.Localization.Translate("Error_Notes_Not_Found"));
                    return false;
                }
                
            }
            catch (Exception ex)
            {
                // Tratar exceções
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Note_Import"));
                return false;
            }
        }

        private static bool IsValidJson(string jsonString)
        {
            try
            {
                System.Text.Json.JsonDocument.Parse(jsonString);
                return true;
            }
            catch
            {
                return false;
            }
        }

        public static void UpdateCurrentNote(string jsonString)
        {
            // Busca a nota atual com IsCurrentNote = true
            var currentNote = DatabaseHelper.Read<NoteModel>().FirstOrDefault();

            if (currentNote != null)
            {
                // Atualiza o campo NoteJson com o novo JSON recebido
                currentNote.NoteJson = jsonString;
                currentNote.UpdatedAt = DateTime.UtcNow; // Atualiza a data de modificação

                // Atualiza a nota no banco de dados
                bool updateSuccess = DatabaseHelper.Update(currentNote);

                if (updateSuccess)
                {
                    string currentDate;

                    if (((App)Application.Current).AppLanguage == "pt_br")
                        currentDate = DateTime.Now.ToString("dd/MM/yyyy H:mm:ss");
                    else
                        currentDate = DateTime.Now.ToString("MM/dd/yyyy h:mm:ss tt");

                    ((App)Application.Current).LastNoteUpdated = App.Localization.Translate("Last_Save_Note") + currentDate;

                    CreateNewBackupNote(jsonString, currentDate);
                }
                else
                {
                    ModalMessages.showErrorModal(App.Localization.Translate("Error_App_Update_Note"));
                }
            }
            else
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_App_Update_Note"));
            }
        }

        private static void CreateNewBackupNote(string jsonString, string dateTime)
        {
            try
            {
                // Define o caminho da pasta de backups
                string backupFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MinimalTextEditorLite",
                    "Backups"
                );

                // Cria a pasta se ela não existir
                if (!Directory.Exists(backupFolder))
                {
                    Directory.CreateDirectory(backupFolder);
                }

                // Define o nome do arquivo de backup com base na data/hora
                string backupFileName = $"backup_{dateTime.Replace('/', '-').Replace(':', '-')}.json";
                string backupFilePath = Path.Combine(backupFolder, backupFileName);

                // Salva o conteúdo do JSON no arquivo de backup
                File.WriteAllText(backupFilePath, jsonString);
            }
            catch (Exception ex)
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Backup_File"));
            }
        }

        public static (string FileCount, string TotalSize) GetBackupFolderStatistics()
        {
            try
            {
                // Define o caminho da pasta de backups
                string backupFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MinimalTextEditorLite",
                    "Backups"
                );

                if (!Directory.Exists(backupFolder))
                {
                    return ("0", "0 MB");
                }

                // Obtém todos os arquivos na pasta
                var files = Directory.GetFiles(backupFolder);
                long totalSizeBytes = files.Sum(file => new FileInfo(file).Length);

                // Calcula o tamanho total em MB ou GB
                string totalSize;
                if (totalSizeBytes >= 1_073_741_824) // 1 GB
                {
                    totalSize = $"{totalSizeBytes / 1_073_741_824.0:F2} GB";
                }
                else
                {
                    totalSize = $"{totalSizeBytes / 1_048_576.0:F2} MB";
                }

                return ($"{files.Length}", totalSize);
            }
            catch (Exception ex)
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Backup_Folder_Stats"));
                return ("Error", "Error");
            }
        }

        public static void RemoveBackupFiles()
        {
            try
            {
                // Define o caminho da pasta de backups
                string backupFolder = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                    "MinimalTextEditorLite",
                    "Backups"
                );

                if (Directory.Exists(backupFolder))
                {
                    // Remove todos os arquivos dentro da pasta
                    var files = Directory.GetFiles(backupFolder);
                    foreach (var file in files)
                    {
                        File.Delete(file);
                    }
                }
                else
                {
                    ModalMessages.showErrorModal(App.Localization.Translate("Error_Backup_Folder_Exists"));
                }
            }
            catch (Exception ex)
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Backup_Files_Remove"));
            }
        }
    }
}
