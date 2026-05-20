using MinimalTextEditorLite.Exporters.Contracts.EditorJs;
using System.Diagnostics;
using System.Text.Json;

namespace MinimalTextEditorLite.Core.Exporters;

public sealed class PdfExporter : IExporter
{
    public string Id => "pdf";
    public string DisplayName => "PDF";
    public string DefaultFileName => "Note.pdf";
    public string FileDialogFilter => "PDF Files (*.pdf)|*.pdf";

    public Task<byte[]> ExportAsync(EditorJsDocument document)
    {
        var toolPath = Path.Combine(AppContext.BaseDirectory, "Modules", "Export", "x64", "ExportAsPDF.exe");
        return ExportWithExternalToolAsync(document, toolPath);
    }

    private static async Task<byte[]> ExportWithExternalToolAsync(EditorJsDocument document, string toolPath)
    {
        string? tempJsonFilePath = null;

        try
        {
            tempJsonFilePath = Path.GetTempFileName();
            var json = JsonSerializer.Serialize(document, EditorJsJson.Options);
            await File.WriteAllTextAsync(tempJsonFilePath, json);

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
            await process.StandardOutput.BaseStream.CopyToAsync(memoryStream);
            await process.WaitForExitAsync();

            if (process.ExitCode != 0)
            {
                var error = await process.StandardError.ReadToEndAsync();
                throw new InvalidOperationException(error);
            }

            return memoryStream.ToArray();
        }
        finally
        {
            if (tempJsonFilePath != null && File.Exists(tempJsonFilePath))
                File.Delete(tempJsonFilePath);
        }
    }
}
