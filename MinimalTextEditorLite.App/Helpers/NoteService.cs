using Microsoft.Win32;
using MinimalTextEditorLite.Core.Database;
using MinimalTextEditorLite.Core.Models;
using MinimalTextEditorLite.Exporters.Contracts.EditorJs;
using System.Diagnostics;
using System.IO;
using System.Text;
using System.Text.Json;

namespace MinimalTextEditorLite.App.Helpers;

public sealed class NoteService(IDatabaseHelper database)
{
    public async Task<bool> ExportCurrentNoteAsJSON()
    {
        try
        {
            var note = database.QuerySingle<NoteModel>("SELECT * FROM Note WHERE Id = ?", 1);

            if (note == null)
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Notes_Not_Found"));
                return false;
            }

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                DefaultExt = "json",
                FileName = "Note.json",
                Title = App.Localization.Translate("Title_Export_Note")
            };

            if (saveFileDialog.ShowDialog() == true)
            {
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

    public async Task<bool> ExportCurrentNoteAsDoc()
    {
        return await ExportWithExternalTool("Modules\\Export\\x64\\ExportAsDOC.exe", "Word Documents (*.docx)|*.docx", "docx", "Note.docx");
    }

    public async Task<bool> ExportCurrentNoteAsPDF()
    {
        return await ExportWithExternalTool("Modules\\Export\\x64\\ExportAsPDF.exe", "Word Documents (*.pdf)|*.pdf", "pdf", "Note.pdf");
    }

    private async Task<bool> ExportWithExternalTool(string toolPath, string filter, string extension, string fileName)
    {
        string? tempJsonFilePath = null;

        try
        {
            var note = database.QuerySingle<NoteModel>("SELECT * FROM Note WHERE Id = ?", 1);

            if (note == null)
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Notes_Not_Found"));
                return false;
            }

            tempJsonFilePath = Path.GetTempFileName();
            await File.WriteAllTextAsync(tempJsonFilePath, note.NoteJson);

            var result = await Task.Run(() =>
            {
                var processStartInfo = new ProcessStartInfo
                {
                    FileName = toolPath,
                    Arguments = $"\"{tempJsonFilePath}\"",
                    RedirectStandardOutput = true,
                    RedirectStandardError = true,
                    UseShellExecute = false,
                    CreateNoWindow = true,
                    WorkingDirectory = Path.GetTempPath()
                };

                using var process = new Process { StartInfo = processStartInfo };
                process.Start();

                using var memoryStream = new MemoryStream();
                process.StandardOutput.BaseStream.CopyTo(memoryStream);
                process.WaitForExit();

                if (process.ExitCode != 0)
                {
                    var error = process.StandardError.ReadToEnd();
                    throw new Exception(error);
                }

                return memoryStream.ToArray();
            });

            var saveFileDialog = new SaveFileDialog
            {
                Filter = filter,
                DefaultExt = extension,
                FileName = fileName,
                Title = App.Localization.Translate("Title_Export_Note")
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                await File.WriteAllBytesAsync(saveFileDialog.FileName, result);
                return true;
            }

            return false;
        }
        catch (Exception ex)
        {
            ModalMessages.showErrorModal($"{App.Localization.Translate("Error_Note_Export")} - {ex.Message}");
            return false;
        }
        finally
        {
            if (tempJsonFilePath != null && File.Exists(tempJsonFilePath))
                File.Delete(tempJsonFilePath);
        }
    }

    public async Task<bool> ExportCurrentNoteAsHTML()
    {
        try
        {
            var note = database.QuerySingle<NoteModel>("SELECT * FROM Note WHERE Id = ?", 1);

            if (note == null)
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Notes_Not_Found"));
                return false;
            }

            var noteJson = JsonSerializer.Deserialize<EditorJsDocument>(note.NoteJson, EditorJsJson.Options);
            if (noteJson == null)
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Invalid_Json"));
                return false;
            }

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

            foreach (var block in noteJson.Blocks)
            {
                switch (block.Type)
                {
                    case "header":
                    {
                        var data = block.Data.Deserialize<EditorJsHeaderData>(EditorJsJson.Options);
                        var level = Math.Clamp(data?.Level ?? 1, 1, 6);
                        htmlBuilder.Append($"<h{level}>{data?.Text ?? string.Empty}</h{level}>");
                        break;
                    }
                    case "paragraph":
                    {
                        var data = block.Data.Deserialize<EditorJsParagraphData>(EditorJsJson.Options);
                        htmlBuilder.Append($"<p>{data?.Text ?? string.Empty}</p>");
                        break;
                    }
                    case "list":
                    {
                        var data = block.Data.Deserialize<EditorJsListData>(EditorJsJson.Options);
                        var listTag = string.Equals(data?.Style, "ordered", StringComparison.OrdinalIgnoreCase) ? "ol" : "ul";
                        htmlBuilder.Append($"<{listTag}>");

                        foreach (var item in data?.Items ?? [])
                            htmlBuilder.Append($"<li>{item}</li>");

                        htmlBuilder.Append($"</{listTag}>");
                        break;
                    }
                    case "checklist":
                    {
                        var data = block.Data.Deserialize<EditorJsChecklistData>(EditorJsJson.Options);
                        htmlBuilder.Append("<ul>");

                        foreach (var item in data?.Items ?? [])
                        {
                            var checkbox = item.Checked ? "☑" : "☐";
                            htmlBuilder.Append($"{checkbox} {item.Text}<br>");
                        }

                        htmlBuilder.Append("</ul>");
                        break;
                    }
                    case "quote":
                    {
                        var data = block.Data.Deserialize<EditorJsQuoteData>(EditorJsJson.Options);
                        htmlBuilder.Append($"<blockquote><p>{data?.Text ?? string.Empty}</p><footer>- {data?.Caption ?? string.Empty}</footer></blockquote>");
                        break;
                    }
                    case "warning":
                    {
                        var data = block.Data.Deserialize<EditorJsWarningData>(EditorJsJson.Options);
                        htmlBuilder.Append("<div style='border: 1px solid #ffa500; padding: 10px; margin: 10px 0; background: #fff8e5;'>");
                        htmlBuilder.Append($"<strong>{data?.Title ?? string.Empty}</strong>: {data?.Message ?? string.Empty}");
                        htmlBuilder.Append("</div>");
                        break;
                    }
                    case "code":
                    {
                        var data = block.Data.Deserialize<EditorJsCodeData>(EditorJsJson.Options);
                        htmlBuilder.Append($"<pre><code>{System.Security.SecurityElement.Escape(data?.Code ?? string.Empty)}</code></pre>");
                        break;
                    }
                    case "delimiter":
                    {
                        htmlBuilder.Append("<p style='text-align:center; font-size:28px; margin:15px;'>***</p>");
                        break;
                    }
                    case "table":
                    {
                        var data = block.Data.Deserialize<EditorJsTableData>(EditorJsJson.Options);
                        htmlBuilder.Append("<table>");

                        foreach (var row in data?.Content ?? [])
                        {
                            htmlBuilder.Append("<tr>");

                            foreach (var cell in row)
                                htmlBuilder.Append($"<td>{cell}</td>");

                            htmlBuilder.Append("</tr>");
                        }

                        htmlBuilder.Append("</table>");
                        break;
                    }
                    case "image":
                    {
                        var data = block.Data.Deserialize<EditorJsImageData>(EditorJsJson.Options);
                        htmlBuilder.Append("<figure>");
                        htmlBuilder.Append($"<img src=\"{data?.Url ?? string.Empty}\" alt=\"{data?.Caption ?? string.Empty}\">");

                        if (!string.IsNullOrEmpty(data?.Caption))
                            htmlBuilder.Append($"<figcaption>{data.Caption}</figcaption>");

                        htmlBuilder.Append("</figure>");
                        break;
                    }
                }
            }

            htmlBuilder.Append("</body>");
            htmlBuilder.Append("</html>");

            var saveFileDialog = new SaveFileDialog
            {
                Filter = "HTML Files (*.html)|*.html",
                DefaultExt = "html",
                FileName = "Note.html",
                Title = App.Localization.Translate("Title_Export_Note")
            };

            if (saveFileDialog.ShowDialog() == true)
            {
                await File.WriteAllTextAsync(saveFileDialog.FileName, htmlBuilder.ToString());
                return true;
            }

            return false;
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Note_Export"));
            return false;
        }
    }

    public bool OpenNewNote()
    {
        try
        {
            var openFileDialog = new OpenFileDialog
            {
                Filter = "JSON Files (*.json)|*.json",
                DefaultExt = "json",
                Title = App.Localization.Translate("Title_Open_Note")
            };

            if (openFileDialog.ShowDialog() != true)
                return false;

            string jsonContent = File.ReadAllText(openFileDialog.FileName);

            if (!IsValidJson(jsonContent))
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Invalid_Json"));
                return false;
            }

            using var jsonObject = JsonDocument.Parse(jsonContent);
            if (!jsonObject.RootElement.TryGetProperty("blocks", out _))
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Invalid_Json_Key_Block"));
                return false;
            }

            var note = database.QuerySingle<NoteModel>("SELECT * FROM Note WHERE Id = ?", 1);

            if (note != null)
            {
                note.NoteJson = jsonContent;
                note.UpdatedAt = DateTime.UtcNow;
                database.Update(note);
                return true;
            }

            ModalMessages.showErrorModal(App.Localization.Translate("Error_Notes_Not_Found"));
            return false;
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Note_Import"));
            return false;
        }
    }

    private static bool IsValidJson(string jsonString)
    {
        try
        {
            using var _ = JsonDocument.Parse(jsonString);
            return true;
        }
        catch
        {
            return false;
        }
    }

    public void UpdateCurrentNote(string jsonString)
    {
        var currentNote = database.Read<NoteModel>().FirstOrDefault();

        if (currentNote != null)
        {
            currentNote.NoteJson = jsonString;
            currentNote.UpdatedAt = DateTime.UtcNow;

            bool updateSuccess = database.Update(currentNote);

            if (updateSuccess)
            {
                string currentDate = ((App)System.Windows.Application.Current).AppLanguage == "pt_br"
                    ? DateTime.Now.ToString("dd/MM/yyyy H:mm:ss")
                    : DateTime.Now.ToString("MM/dd/yyyy h:mm:ss tt");

                ((App)System.Windows.Application.Current).LastNoteUpdated = App.Localization.Translate("Last_Save_Note") + currentDate;
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

    private void CreateNewBackupNote(string jsonString, string dateTime)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.BackupsFolder);
            string backupFileName = $"backup_{dateTime.Replace('/', '-').Replace(':', '-')}.json";
            string backupFilePath = Path.Combine(AppPaths.BackupsFolder, backupFileName);
            File.WriteAllText(backupFilePath, jsonString);
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Backup_File"));
        }
    }

    public (string FileCount, string TotalSize) GetBackupFolderStatistics()
    {
        try
        {
            if (!Directory.Exists(AppPaths.BackupsFolder))
                return ("0", "0 MB");

            var files = Directory.GetFiles(AppPaths.BackupsFolder);
            long totalSizeBytes = files.Sum(file => new FileInfo(file).Length);

            string totalSize = totalSizeBytes >= 1_073_741_824
                ? $"{totalSizeBytes / 1_073_741_824.0:F2} GB"
                : $"{totalSizeBytes / 1_048_576.0:F2} MB";

            return ($"{files.Length}", totalSize);
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Backup_Folder_Stats"));
            return ("Error", "Error");
        }
    }

    public void RemoveBackupFiles()
    {
        try
        {
            if (Directory.Exists(AppPaths.BackupsFolder))
            {
                foreach (var file in Directory.GetFiles(AppPaths.BackupsFolder))
                    File.Delete(file);
            }
            else
            {
                ModalMessages.showErrorModal(App.Localization.Translate("Error_Backup_Folder_Exists"));
            }
        }
        catch
        {
            ModalMessages.showErrorModal(App.Localization.Translate("Error_Backup_Files_Remove"));
        }
    }
}
