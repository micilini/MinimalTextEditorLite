using MinimalTextEditorLite.Exporters.Contracts.EditorJs;
using MinimalTextEditorLite.Core.Security;
using System.Diagnostics;
using System.Text.Json;

namespace MinimalTextEditorLite.Core.Exporters;

public sealed class PdfExporter(IIsolatedTempFileService tempFileService) : IExporter
{
    public string Id => "pdf";
    public string DisplayName => "PDF";
    public string DefaultFileName => "Note.pdf";
    public string FileDialogFilter => "PDF Files (*.pdf)|*.pdf";

    public Task<byte[]> ExportAsync(ExportContext context)
    {
        var toolPath = Path.Combine(AppContext.BaseDirectory, "Modules", "Export", "x64", "ExportAsPDF.exe");
        return ExportWithExternalToolAsync(context.Document, toolPath);
    }

    private async Task<byte[]> ExportWithExternalToolAsync(EditorJsDocument document, string toolPath)
    {
        string? tempJsonFilePath = null;

        try
        {
            if (!File.Exists(toolPath))
                throw new FileNotFoundException("Exporter executable was not found.", toolPath);

            tempJsonFilePath = tempFileService.CreateTempJsonPath();
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
                WorkingDirectory = AppContext.BaseDirectory
            };

            using var process = new Process { StartInfo = processStartInfo };
            using var memoryStream = new MemoryStream();

            process.Start();

            var stdoutTask = process.StandardOutput.BaseStream.CopyToAsync(memoryStream);
            var stderrTask = process.StandardError.ReadToEndAsync();
            var waitTask = process.WaitForExitAsync();

            await Task.WhenAll(stdoutTask, stderrTask, waitTask);

            var stderr = await stderrTask;

            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(
                    string.IsNullOrWhiteSpace(stderr)
                        ? $"Exporter failed with exit code {process.ExitCode}."
                        : stderr);
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
