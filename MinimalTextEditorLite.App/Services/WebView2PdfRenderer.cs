using Microsoft.Web.WebView2.Core;
using Microsoft.Web.WebView2.Wpf;
using MinimalTextEditorLite.Core.Rendering;
using MinimalTextEditorLite.Core.Security;
using System.IO;
using System.Text;
using System.Threading;
using System.Windows;
using System.Windows.Threading;

namespace MinimalTextEditorLite.App.Services;

public sealed class WebView2PdfRenderer : IPdfRenderer
{
    private const string ReadySignal = "mte:print-ready";

    private readonly IIsolatedTempFileService tempFileService;

    public WebView2PdfRenderer(IIsolatedTempFileService tempFileService)
    {
        this.tempFileService = tempFileService;
    }

    public Task<byte[]> RenderHtmlToPdfAsync(string html, PdfRenderOptions options)
    {
        ArgumentNullException.ThrowIfNull(html);
        ArgumentNullException.ThrowIfNull(options);

        var application = Application.Current
            ?? throw new InvalidOperationException("PDF rendering requires an active WPF Application.");

        var dispatcher = application.Dispatcher;

        if (dispatcher.CheckAccess())
            return RenderOnUiThreadAsync(html, options);

        return dispatcher
            .InvokeAsync(() => RenderOnUiThreadAsync(html, options), DispatcherPriority.Background)
            .Task
            .Unwrap();
    }

    private async Task<byte[]> RenderOnUiThreadAsync(string html, PdfRenderOptions options)
    {
        string? htmlPath = null;
        string? pdfPath = null;
        Window? hostWindow = null;
        WebView2? webView = null;

        try
        {
            htmlPath = tempFileService.CreateTempFilePath(".html");
            pdfPath = tempFileService.CreateTempFilePath(".pdf");

            await File.WriteAllTextAsync(htmlPath, html, new UTF8Encoding(encoderShouldEmitUTF8Identifier: false));

            hostWindow = CreateHostWindow();
            webView = new WebView2();
            hostWindow.Content = webView;
            hostWindow.Show();

            await webView.EnsureCoreWebView2Async();
            ConfigureWebView(webView.CoreWebView2);

            using var cts = new CancellationTokenSource(options.Timeout);
            var readyTcs = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);

            using var timeoutRegistration = cts.Token.Register(() =>
            {
                readyTcs.TrySetException(new TimeoutException(
                    "PDF rendering timed out before the HTML document signaled it was ready."));
            });

            void OnMessageReceived(object? sender, CoreWebView2WebMessageReceivedEventArgs args)
            {
                if (args.TryGetWebMessageAsString() == ReadySignal)
                    readyTcs.TrySetResult(true);
            }

            void OnNavigationCompleted(object? sender, CoreWebView2NavigationCompletedEventArgs args)
            {
                if (!args.IsSuccess)
                {
                    readyTcs.TrySetException(new InvalidOperationException(
                        $"PDF HTML navigation failed with WebView2 status: {args.WebErrorStatus}."));
                }
            }

            webView.CoreWebView2.WebMessageReceived += OnMessageReceived;
            webView.CoreWebView2.NavigationCompleted += OnNavigationCompleted;

            try
            {
                webView.CoreWebView2.Navigate(new Uri(htmlPath).AbsoluteUri);

                await readyTcs.Task;

                var printSettings = webView.CoreWebView2.Environment.CreatePrintSettings();
                printSettings.PageWidth = options.PageWidthInches;
                printSettings.PageHeight = options.PageHeightInches;
                printSettings.MarginTop = options.MarginInches;
                printSettings.MarginBottom = options.MarginInches;
                printSettings.MarginLeft = options.MarginInches;
                printSettings.MarginRight = options.MarginInches;
                printSettings.ShouldPrintBackgrounds = options.PrintBackgrounds;
                printSettings.ShouldPrintHeaderAndFooter = options.PrintHeaderFooter;
                printSettings.Orientation = CoreWebView2PrintOrientation.Portrait;

                var success = await webView.CoreWebView2.PrintToPdfAsync(pdfPath, printSettings);
                if (!success)
                    throw new InvalidOperationException("WebView2 reported PrintToPdfAsync failure.");

                return await File.ReadAllBytesAsync(pdfPath);
            }
            finally
            {
                if (webView.CoreWebView2 != null)
                {
                    webView.CoreWebView2.WebMessageReceived -= OnMessageReceived;
                    webView.CoreWebView2.NavigationCompleted -= OnNavigationCompleted;
                }
            }
        }
        finally
        {
            try
            {
                webView?.Dispose();
            }
            catch
            {
                // Best-effort cleanup.
            }

            try
            {
                if (hostWindow != null)
                {
                    hostWindow.Content = null;
                    hostWindow.Close();
                }
            }
            catch
            {
                // Best-effort cleanup.
            }

            TryDeleteTempFile(htmlPath);
            TryDeleteTempFile(pdfPath);
        }
    }

    private static Window CreateHostWindow()
    {
        return new Window
        {
            Width = 800,
            Height = 600,
            Left = -32000,
            Top = -32000,
            WindowStyle = WindowStyle.None,
            ResizeMode = ResizeMode.NoResize,
            ShowInTaskbar = false,
            ShowActivated = false,
            Visibility = Visibility.Visible
        };
    }

    private static void ConfigureWebView(CoreWebView2 coreWebView)
    {
        coreWebView.Settings.AreDefaultContextMenusEnabled = false;
        coreWebView.Settings.AreDevToolsEnabled = false;
        coreWebView.Settings.IsStatusBarEnabled = false;
        coreWebView.Settings.IsZoomControlEnabled = false;
    }

    private static void TryDeleteTempFile(string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            if (File.Exists(path))
                File.Delete(path);
        }
        catch
        {
            // Best-effort cleanup. Never fail an export because temp deletion failed.
        }
    }
}
